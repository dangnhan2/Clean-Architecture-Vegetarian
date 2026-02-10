using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Dtos.Request;

namespace Vegetarian.Application.Validator
{
    public class PasswordValidator : AbstractValidator<PasswordRequestDto>
    {
        public PasswordValidator()
        {
            // 1. Kiểm tra trường CurrentPassword (Mật khẩu hiện tại)
            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage("Mật khẩu hiện tại không được để trống.");

            // 2. Kiểm tra trường NewPassword (Mật khẩu mới)
            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("Mật khẩu mới không được để trống.")
                .MinimumLength(6).WithMessage("Mật khẩu mới phải có ít nhất 6 ký tự.");

            // 3. Kiểm tra trường ConfirmPassword (Xác nhận Mật khẩu)
            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Xác nhận mật khẩu không được để trống.")
                .Equal(x => x.NewPassword).WithMessage("Xác nhận mật khẩu không khớp với Mật khẩu mới.");
        }
    }
}
