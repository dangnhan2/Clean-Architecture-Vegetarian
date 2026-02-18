using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Dtos.Request;

namespace Vegetarian.Application.Validator
{
    public class MenuValidator : AbstractValidator<MenuRequestDto>
    {
        public MenuValidator()
        {
            RuleFor(x => x.Name)
               .NotEmpty().WithMessage("Tên món ăn không được để trống.")
               .MaximumLength(150).WithMessage("Tên món ăn không được vượt quá 150 ký tự.");

            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("Vui lòng chọn danh mục cho món ăn.");

            RuleFor(x => x.OriginalPrice)
                .GreaterThan(0).WithMessage("Giá gốc phải lớn hơn 0.");

            RuleFor(x => x.Description)
               .MaximumLength(500).WithMessage("Tên món ăn không được vượt quá 500 ký tự.");
        }
    }
}
