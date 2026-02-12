using DotNetEnv;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Vegetarian.Application.Dtos.Request;
using Vegetarian.Application.Dtos.Response;
using Vegetarian.Application.Implements.Interface;

namespace Vegetarian.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var result = await _authService.LoginAsync(request, HttpContext);
            var response = ApiResponse<AuthResponse>.Success("Đăng nhập thành công", result, StatusCodes.Status200OK);
            return Ok(response);
        }

        [HttpGet("login/google")]
        public IResult LoginWithGoogle()
        {
            var result = _authService.LoginWithGoogle(HttpContext);

            return result;
        }

        [HttpGet("login/google/callback")]
        public async Task<IActionResult> GoogleCallBack()
        {
            Env.Load();
            var result = await _authService.GoogleCallBackAsync(HttpContext);
            return Redirect($"{Env.GetString($"Frontend__URI")}?token={result.AccessToken}");
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            var result = await _authService.RegisterAsync(request);
            var response = ApiResponse<string>.Success("Email đã được gửi. Hãy nhập mã để xác nhận", result, StatusCodes.Status201Created);
            return CreatedAtAction(null, response);
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken()
        {
            var result = await _authService.RefreshTokenAsync(HttpContext);
            var response = ApiResponse<AuthResponse>.Success("Refresh token successfull", result, StatusCodes.Status201Created);
            return CreatedAtAction(null, response);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync(HttpContext);
            var response = ApiResponse<dynamic>.Success("Đăng xuất thành công", null, StatusCodes.Status200OK);
            return Ok(response);
        }

        [HttpPost("password/change")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] PasswordRequestDto request)
        {
            await _authService.ChangePasswordAsync(request);
            var response = ApiResponse<dynamic>.Success("Đổi mật khẩu thành công", null, StatusCodes.Status201Created);
            return CreatedAtAction(null, response);
        }

        [HttpPost("email/verify")]
        public async Task<IActionResult> VerifyEmail([FromBody] EmailVerifyRequestDto request)
        {
            var result = await _authService.VerifyEmail(request);
            var response = ApiResponse<string>.Success("Xác nhận email thành công", result, StatusCodes.Status200OK);
            return Ok(response);

        }

        [HttpPost("password/forgot")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        {
            await _authService.ForgotPasswordAsync(request);
            var response = ApiResponse<dynamic>.Success("Email đã được gửi. Hãy nhập mã để xác nhận", null, StatusCodes.Status200OK);
            return Ok(request);
        }

        [HttpPost("email/resend")]
        public async Task<IActionResult> ResendEmail(ResendEmailRequestDto resendEmailRequest)
        {
            await _authService.ResendEmailAsync(resendEmailRequest);

            var response = ApiResponse<string>.Success("Email đã được gửi. Hãy nhập mã để xác nhận", "", StatusCodes.Status201Created);

            return CreatedAtAction(null, response);
        }

        [HttpPost("password/reset")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
        {
            await _authService.ResetPasswordAsync(request);
            var response = ApiResponse<dynamic>.Success("Thiết lập mật khẩu mới thành công", null, StatusCodes.Status201Created);
            return CreatedAtAction(null, response);
        }
    }
}
