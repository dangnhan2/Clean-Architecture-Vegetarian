using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Dtos.Request;

namespace Vegetarian.Application.Validator
{
    public class AdvertisementValidator : AbstractValidator<AdvertisementRequestDto>
    {
        public AdvertisementValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Tiêu đề quảng cáo không được để trống.")
                .MaximumLength(200).WithMessage("Tiêu đề không được vượt quá 200 ký tự.");

            RuleFor(x => x.TargetKey)
                .NotEmpty()
                .WithMessage("Từ khóa mục tiêu (TargetKey) không được để trống khi đã chọn loại mục tiêu.");

            RuleFor(x => x.StartAt)
                .NotEmpty().WithMessage("Thời gian bắt đầu không được để trống.");

            RuleFor(x => x.EndAt)
                .GreaterThan(x => x.StartAt).WithMessage("Thời gian kết thúc phải sau thời gian bắt đầu.");
        }
    }
}
