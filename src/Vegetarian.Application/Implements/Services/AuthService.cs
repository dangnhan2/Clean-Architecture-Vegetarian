using DotNetEnv;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Abstractions.BackgroundJobs;
using Vegetarian.Application.Abstractions.Email;
using Vegetarian.Application.Abstractions.Token;
using Vegetarian.Application.Dtos.Request;
using Vegetarian.Application.Dtos.Response;
using Vegetarian.Application.Implements.Interface;
using Vegetarian.Application.Validator;
using Vegetarian.Domain.Models;

namespace Vegetarian.Application.Implements.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly ITokenGenerator _tokenService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly string _avatar;
        private readonly IHangfireJobClient _backgroundJobService;

        public AuthService(UserManager<User> userManager, ITokenGenerator tokenService, IUnitOfWork unitOfWork, IHangfireJobClient backgroundJobService)
        {
            Env.Load();
            _userManager = userManager;
            _tokenService = tokenService;
            _unitOfWork = unitOfWork;
            _avatar = Env.GetString("DEFAULT_AVATAR");
            _backgroundJobService = backgroundJobService;
        }
        public async Task ChangePasswordAsync(PasswordRequestDto request)
        {
            var result = await new PasswordValidator().ValidateAsync(request);
            if (!result.IsValid)
                throw new ValidationDictionaryException(result.ToDictionary());

            var user = await _userManager.FindByIdAsync(request.Id.ToString());

            if (user == null)
                throw new KeyNotFoundException("Người dùng không tồn tại");

            var isCorrectlyPassword = await _userManager.CheckPasswordAsync(user, request.CurrentPassword);

            if (!isCorrectlyPassword)
                throw new InvalidDataException("Mật khẩu hiện tại không đúng");

            await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        }

        public async Task<string> ForgotPasswordAsync(ForgotPasswordRequestDto request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
                throw new KeyNotFoundException("Người dùng không tồn tại");

            //Log.Information("Generate otp");
            //generate opt
            var otp = await GenerateOtp(user.Id);

            Log.Information("Send email");

            SendEmail(user.Id, user.Email, otp);

            return user.Email;
        }

        public async Task<AuthResponse> LoginAsync(LoginRequestDto request, HttpContext context)
        {
            var result = await new LoginValidator().ValidateAsync(request);

            if (!result.IsValid)
                throw new ValidationDictionaryException(result.ToDictionary());

            var user = await _userManager.FindByEmailAsync(request.Email);


            var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);

            if (user == null || !isPasswordValid)
                throw new ArgumentException("Thông tin đăng nhập không đúng");

            if (await _userManager.IsLockedOutAsync(user))
                throw new UnauthorizedAccessException("Tài khoản của bạn đã bị khóa , vui lòng liên hệ với admin");

            if (!user.EmailConfirmed)
                throw new ArgumentException("Tài khoản của bạn chưa được xác nhận, hãy xác nhận email để tiếp tục đăng nhập");

            var authResponse = await _tokenService.GenerateToken(user, context);

            return authResponse;
        }

        public async Task LogoutAsync(HttpContext context)
        {
            var refreshToken = context.Request.Cookies["refreshToken"];
            if (refreshToken == null)
                throw new UnauthorizedAccessException(nameof(refreshToken));

            var existToken = await _unitOfWork.RefreshToken.GetTokenByRefreshToken(refreshToken);

            if (existToken == null || existToken.ExpriedAt < DateTime.UtcNow)
                throw new UnauthorizedAccessException(nameof(refreshToken));

            var user = await _unitOfWork.User.GetByIdAsync(existToken.UserId);

            user.RefreshTokens.Clear();

            _unitOfWork.User.Update(user);
            _unitOfWork.RefreshToken.Remove(existToken);
            await _unitOfWork.SaveChangeAsync();

            SetupToken(context);
        }

        public async Task<AuthResponse> RefreshTokenAsync(HttpContext context)
        {
            var response = await _tokenService.GenerateRefreshToken(context);
            return response;
        }

        public async Task<string> RegisterAsync(RegisterRequestDto request)
        {
            var result = await new RegisterValidator().ValidateAsync(request);

            if (!result.IsValid)
                throw new ValidationDictionaryException(result.ToDictionary());

            var isExistUser = await _userManager.FindByEmailAsync(request.Email);

            if (isExistUser != null)
                throw new InvalidDataException("Email đã được đăng kí");

            var newUser = new User
            {
                Id = Guid.NewGuid(),
                UserName = request.UserName,
                ImageUrl = _avatar,
                Email = request.Email,
                NormalizedEmail = request.Email.ToUpper(),
                PhoneNumber = null,
                PhoneNumberConfirmed = false
            };

            var response = await _userManager.CreateAsync(newUser, request.Password);

            //Create an EmailOtp object
            var otp = await GenerateOtp(newUser.Id);

            // Add user to role
            await _userManager.AddToRoleAsync(newUser, "Customer");

            SendEmail(newUser.Id, newUser.Email, otp);

            return request.Email;
        }

        public async Task ResendEmailAsync(ResendEmailRequestDto resendEmailRequest)
        {
            var result = await new ResendEmailValidator().ValidateAsync(resendEmailRequest);

            if (!result.IsValid)
                throw new ValidationDictionaryException(result.ToDictionary());

            var user = await _unitOfWork.User.GetUserByEmailAsync(resendEmailRequest.UserEmail);

            if (user == null)
                throw new KeyNotFoundException("Người dùng không tồn tại");

            foreach (var otp in user.EmailOtps)
            {
                if (!otp.IsUsed)
                    otp.IsUsed = true;
            }

            var newOtp = await GenerateOtp(user.Id);

            SendEmail(user.Id, user.Email, newOtp);
        }

        public async Task ResetPasswordAsync(ResetPasswordRequestDto request)
        {
            var result = await new ResetPasswordValidator().ValidateAsync(request);

            if (!result.IsValid)
                throw new ValidationDictionaryException(result.ToDictionary());

            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
                throw new KeyNotFoundException("Người dùng không tồn tại");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            await _userManager.ResetPasswordAsync(user, token, request.NewPassword);
        }

        public async Task<string> VerifyEmail(EmailVerifyRequestDto request)
        {
            var existUser = await _unitOfWork.User.GetUserByEmailAsync(request.Email);

            if (existUser == null)
                throw new KeyNotFoundException("Người dùng không tồn tại");


            if (existUser.EmailOtps.Any(otp => otp.Otp == request.Otp && otp.IsUsed || otp.Otp == request.Otp && otp.ExpiredAt < DateTime.UtcNow))
                throw new ArgumentException("Mã otp đã sử dụng hoặc đã hết hạn");

            foreach (var otp in existUser.EmailOtps)
            {
                if (otp.Otp == request.Otp)
                {
                    existUser.EmailConfirmed = true;
                    _unitOfWork.EmailOtp.Remove(otp);
                    await _userManager.UpdateAsync(existUser);
                    break;
                }
            }

            await _unitOfWork.SaveChangeAsync();

            return request.Email;
        }

        public IResult LoginWithGoogle(HttpContext context)
        {
            var props = new AuthenticationProperties
            {
                RedirectUri = "/api/auth/login/google/callback"
            };

            return Results.Challenge(props, new[] { GoogleDefaults.AuthenticationScheme });
        }

        public async Task<AuthResponse> GoogleCallBackAsync(HttpContext context)
        {
            try
            {
                // Authenticate với Google scheme thay vì Cookie scheme
                var result = await context.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

                if (!result.Succeeded)
                {
                    throw new Exception("Google authentication failed");
                }

                // Lấy claims
                var claims = result.Principal.Claims.ToList();

                // Debug: Log tất cả claims để xem có gì
                foreach (var claim in claims)
                {
                    Console.WriteLine($"Claim Type: {claim.Type}, Value: {claim.Value}");
                }

                var email = result.Principal?.FindFirstValue(ClaimTypes.Email)
                            ?? result.Principal?.FindFirstValue("email");

                var name = result.Principal?.FindFirstValue(ClaimTypes.Name)
                           ?? result.Principal?.FindFirstValue("name");

                var avatar = result.Principal?.FindFirst("picture")?.Value
                             ?? result.Principal?.FindFirst("urn:google:picture")?.Value;

                if (string.IsNullOrEmpty(email))
                {
                    throw new Exception("Email not found in Google claims");
                }

                var user = await _userManager.FindByEmailAsync(email);

                if (user != null)
                {
                    return await _tokenService.GenerateToken(user, context);
                }

                var newUser = MappingUserWhenLoginWithGoogle(name, email, avatar);
                await _userManager.CreateAsync(newUser);
                await _userManager.AddToRoleAsync(newUser, "Customer");

                var authResponse = await _tokenService.GenerateToken(newUser, context);
                return authResponse;
            }
            catch (Exception ex)
            {
                // Log lỗi cụ thể
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }

        #region helper method
        private void SetupToken(HttpContext context)
        {
            context.Response.Cookies.Append(
                "refreshToken",
                string.Empty,
                new CookieOptions
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.None,
                    Secure = true,
                    Expires = DateTime.UnixEpoch,
                    Path = ""
                });
        }

        private async Task<string> GenerateOtp(Guid userId)
        {
            var otp = new Random().Next(100000, 999999).ToString();

            var emailOtp = new EmailOtp
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                IsUsed = false,
                Otp = otp
            };

            await _unitOfWork.EmailOtp.AddAsync(emailOtp);
            await _unitOfWork.SaveChangeAsync();

            return otp;

        }

        private void SendEmail(Guid userId, string email, string otp)
        {
            var htmlBody = $"<p>Mã xác nhận email của bạn là:</p> " +
                $" <p class=\"otp\">{otp}</p> " +
                $"<p>Mã sẽ hết hạn trong {5} phút. Không chia sẻ mã otp này cho bất kì ai</p>";

            _backgroundJobService.Enqueue<IEmailSender>(x => x.Sender(email, "Một email đã gửi đến email của bạn . Hãy nhập mã xác nhận", htmlBody));
        }

        private User MappingUserWhenLoginWithGoogle(string userName, string email, string avatar)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = userName,
                ImageUrl = avatar,
                Email = email,
                NormalizedEmail = email.ToUpper(),
                PhoneNumber = null,
                EmailConfirmed = true,
            };

            return user;
        }

        
        #endregion
    }
}
