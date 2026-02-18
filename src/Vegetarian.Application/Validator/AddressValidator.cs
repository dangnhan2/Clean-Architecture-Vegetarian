using FluentValidation;
using FluentValidation.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Dtos.Request;

namespace Vegetarian.Application.Validator
{
    public class AddressValidator : AbstractValidator<AddressRequestDto>
    {
        public AddressValidator()
        {
            RuleFor(x => x.UserId)
             .NotEmpty().WithMessage("ID người dùng không được để trống.");

            // 2. Kiểm tra trường Address
            RuleFor(x => x.Address)
                .NotEmpty().WithMessage("Địa chỉ chi tiết không được để trống.")
                .MaximumLength(250).WithMessage("Địa chỉ không được vượt quá 250 ký tự.");

            // 3. Kiểm tra trường FullName
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Họ và tên người nhận không được để trống.")
                .MaximumLength(100).WithMessage("Họ và tên không được vượt quá 100 ký tự.");

            // 4. Kiểm tra trường PhoneNumber
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Số điện thoại không được để trống.")
                .Matches(@"^0\d{9,10}$").WithMessage("Số điện thoại không hợp lệ. Vui lòng nhập đúng định dạng (ví dụ: 0987654321).");
        }
    }
}
