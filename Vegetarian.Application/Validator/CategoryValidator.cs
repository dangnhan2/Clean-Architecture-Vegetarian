using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Dtos.Request;

namespace Vegetarian.Application.Validator
{
    public class CategoryValidator : AbstractValidator<CategoryRequestDto>
    {
        public CategoryValidator()
        {
            RuleFor(c => c.Name)
                  .NotEmpty().WithMessage("Tên menu không được để trống")
                  .MaximumLength(100).WithMessage("Tên menu không được dài quá 100 kí tự");
        }
    }
}
