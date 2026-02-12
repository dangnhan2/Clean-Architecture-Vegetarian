using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Dtos.Request;

namespace Vegetarian.Application.Validator
{
    public class ResponseRatingValidator : AbstractValidator<ResponseRatingRequestDto>
    {
        public ResponseRatingValidator()
        {
            // 1. Kiểm tra UserId (Người thực hiện phản hồi)
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("ID người dùng không được để trống.");

            // 2. Kiểm tra RatingId (Phản hồi cho đánh giá nào)
            RuleFor(x => x.RatingId)
                .NotEmpty().WithMessage("ID đánh giá không được để trống.");

            // 3. Kiểm tra nội dung phản hồi (Comment)
            RuleFor(x => x.Comment)
                .NotEmpty().WithMessage("Nội dung phản hồi không được để trống.")
                .MinimumLength(10).WithMessage("Phản hồi quá ngắn, vui lòng nhập ít nhất 10 ký tự.")
                .MaximumLength(1000).WithMessage("Nội dung phản hồi không được vượt quá 1000 ký tự.");
        }
    }
}
