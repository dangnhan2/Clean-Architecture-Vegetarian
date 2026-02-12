using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Dtos.Request;

namespace Vegetarian.Application.Validator
{
    public class RatingValidator : AbstractValidator<RatingRequestDto>
    {
        private const int MaxImageCount = 3;

        public RatingValidator()
        {
            RuleFor(x => x.MenuId)
            .NotEmpty().WithMessage("ID món ăn không được để trống.");

            // 2. Kiểm tra trường OrderId
            RuleFor(x => x.OrderId)
                .NotEmpty().WithMessage("ID đơn hàng không được để trống.");

            // 3. Kiểm tra trường UserId
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("ID người dùng không được để trống.");

            // 4. Kiểm tra trường Stars (Điểm số)
            RuleFor(x => x.Stars)
                .InclusiveBetween(1, 5).WithMessage("Điểm đánh giá phải nằm trong khoảng từ 1 đến 5 sao.");

            // 5. Kiểm tra trường Comment
            RuleFor(x => x.Comment)
                .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Comment))
                .WithMessage("Bình luận không được vượt quá 500 ký tự.");

            // 6. Kiểm tra danh sách Images (Tệp ảnh)
            RuleFor(x => x.Images)
                .Must(list => list.Count <= MaxImageCount).WithMessage($"Bạn chỉ có thể đính kèm tối đa {MaxImageCount} ảnh.");
        }
    }
}
