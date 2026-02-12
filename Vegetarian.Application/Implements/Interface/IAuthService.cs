using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Dtos.Request;
using Vegetarian.Application.Dtos.Response;

namespace Vegetarian.Application.Implements.Interface
{
    public interface IAuthService
    {
        public Task<string> RegisterAsync(RegisterRequestDto request);
        public Task<AuthResponse> LoginAsync(LoginRequestDto request, HttpContext context);
        public IResult LoginWithGoogle(HttpContext context);
        public Task<AuthResponse> GoogleCallBackAsync(HttpContext context);
        public Task<AuthResponse> RefreshTokenAsync(HttpContext context);
        public Task LogoutAsync(HttpContext context);
        public Task<string> VerifyEmail(EmailVerifyRequestDto request);
        public Task ResendEmailAsync(ResendEmailRequestDto resendEmailRequest);
        public Task ChangePasswordAsync(PasswordRequestDto request);
        public Task ForgotPasswordAsync(ForgotPasswordRequestDto request);
        public Task ResetPasswordAsync(ResetPasswordRequestDto request);
    }
}
