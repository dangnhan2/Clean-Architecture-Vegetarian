using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Dtos.Request;

namespace Vegetarian.Application.Validator
{
    public class RegisterValidator : AbstractValidator<RegisterRequestDto>
    {
        public RegisterValidator()
        {
            // 1. Kiểm tra Email
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Địa chỉ email không được để trống.")
                .EmailAddress().WithMessage("Định dạng email không hợp lệ.");

            // 2. Kiểm tra UserName
            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage("Tên đăng nhập không được để trống.")
                .MinimumLength(5).WithMessage("Tên đăng nhập phải có ít nhất 6 ký tự.");

            // 3. Kiểm tra Password
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Mật khẩu không được để trống.")
                .MinimumLength(6).WithMessage("Mật khẩu phải có ít nhất 6 ký tự.");

            // 4. Kiểm tra ConfirmPassword (Xác nhận mật khẩu)
            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Vui lòng xác nhận lại mật khẩu.")
                .Equal(x => x.Password).WithMessage("Mật khẩu xác nhận không khớp với mật khẩu đã nhập.");

        }
    }
}
