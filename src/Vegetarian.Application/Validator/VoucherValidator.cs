using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Dtos.Request;

namespace Vegetarian.Application.Validator
{
    public class VoucherValidator : AbstractValidator<VoucherRequestDto>
    {
        public VoucherValidator()
        {
            RuleFor(x => x.Code)
             .NotEmpty().WithMessage("Mã giảm giá không được để trống.")
             .Length(4, 20).WithMessage("Mã giảm giá phải dài từ 4 đến 20 ký tự.");

            // 2. Kiểm tra trường DiscountType
            var validDiscountTypes = new[] { "percent", "fixed" };
            RuleFor(x => x.DiscountType)
                .NotEmpty().WithMessage("Loại giảm giá không được để trống.")
                .Must(type => validDiscountTypes.Contains(type.ToLower()))
                .WithMessage($"Loại giảm giá không hợp lệ. Chỉ chấp nhận '{validDiscountTypes[0]}' hoặc '{validDiscountTypes[1]}'.");

            // 3. Kiểm tra trường DiscountValue
            RuleFor(x => x.DiscountValue)
                .GreaterThan(0).WithMessage("Giá trị giảm giá phải lớn hơn 0.");

            // 4. Kiểm tra MaxDiscount và MinOrderAmount
            RuleFor(x => x.MaxDiscount)
                .GreaterThanOrEqualTo(0).WithMessage("Giới hạn giảm tối đa không được là số âm.");

            RuleFor(x => x.MinOrderAmount)
                .GreaterThanOrEqualTo(0).WithMessage("Giá trị đơn hàng tối thiểu không được là số âm.");

            // 5. Logic có điều kiện: Nếu là giảm theo PERCENT
            When(x => x.DiscountType.ToLower() == "percent", () =>
            {
                RuleFor(x => x.DiscountValue)
                    .InclusiveBetween(1, 100).WithMessage("Giá trị phải nằm trong khoảng từ 1 đến 100.");

                // Đảm bảo MaxDiscount không thể nhỏ hơn 0
                RuleFor(x => x.MaxDiscount)
                    .GreaterThan(0).WithMessage("Giới hạn giảm tối đa (MaxDiscount) phải lớn hơn 0.");
            });

            //Logic có điều kiện: Nếu là giảm theo FIXED
            When(x => x.DiscountType.ToLower() == "fixed", () =>
            {
                RuleFor(x => x.MaxDiscount)
                    .Equal(x => x.DiscountValue)
                    .WithMessage("Đối với loại giảm giá cố định, Giới hạn giảm tối đa (MaxDiscount) phải bằng Giá trị giảm giá (DiscountValue).");
            });

            // 6. Kiểm tra ngày StartDate và EndDate
            RuleFor(x => x.StartDate)
                .NotEmpty().WithMessage("Ngày bắt đầu không được để trống.");

            RuleFor(x => x.EndDate)
                .NotEmpty().WithMessage("Ngày kết thúc không được để trống.")
                .GreaterThan(x => x.StartDate).WithMessage("Ngày kết thúc phải sau Ngày bắt đầu.");

            // 7. Kiểm tra trường UsageLimit và PerUserLimit
            RuleFor(x => x.UsageLimit)
                .GreaterThanOrEqualTo(0).When(x => x.UsageLimit.HasValue)
                .WithMessage("Giới hạn sử dụng toàn bộ không được là số âm.");

            RuleFor(x => x.PerUserLimit)
                .GreaterThanOrEqualTo(0).When(x => x.PerUserLimit.HasValue)
                .WithMessage("Giới hạn sử dụng cho mỗi người dùng không được là số âm.");
        }
    }
}
