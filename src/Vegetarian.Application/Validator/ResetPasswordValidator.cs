using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Dtos.Request;

namespace Vegetarian.Application.Validator
{
    public class ResetPasswordValidator : AbstractValidator<ResetPasswordRequestDto>
    {
        public ResetPasswordValidator()
        {
            // 1. Kiểm tra trường Email
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Địa chỉ email không được để trống.")
                .EmailAddress().WithMessage("Định dạng email không hợp lệ.");

            // 2. Kiểm tra trường Password (Thiết lập quy tắc bảo mật mạnh)
            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("Mật khẩu không được để trống.")
                .MinimumLength(6).WithMessage("Mật khẩu phải có ít nhất 6 ký tự.");

            // 3. Kiểm tra trường ConfirmPassword (Xác nhận Mật khẩu)
            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Xác nhận mật khẩu không được để trống.")
                .Equal(x => x.NewPassword).WithMessage("Xác nhận mật khẩu không khớp với Mật khẩu đã nhập.");
        }
    }
}
