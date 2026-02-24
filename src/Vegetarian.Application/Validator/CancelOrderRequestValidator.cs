using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Dtos.Request;
using Vegetarian.Domain.Enum;

namespace Vegetarian.Application.Validator
{
    public class CancelOrderRequestValidator : AbstractValidator<CancelOrderRequestDto>
    {
        public CancelOrderRequestValidator() {
            RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Vui lòng nhập lý do hủy đơn hàng.")
            .MinimumLength(10).WithMessage("Lý do hủy đơn phải có ít nhất 10 ký tự.");

            // 2. Logic điều kiện cho thanh toán QR
            When(x => x.PaymentMethod == PaymentMethod.QR, () =>
            {
                RuleFor(x => x.BankBin)
                    .NotEmpty().WithMessage("Khi thanh toán qua QR, bạn phải cung cấp mã BIN ngân hàng để nhận hoàn tiền.");

                RuleFor(x => x.BankAccountNumber)
                    .NotEmpty().WithMessage("Khi thanh toán qua QR, bạn phải cung cấp số tài khoản ngân hàng.")
                    .Matches(@"^\d{6,18}$").WithMessage("Số tài khoản ngân hàng không hợp lệ (phải từ 6-18 chữ số).");
            });

            // 3. Nếu không phải QR, có thể cấm nhập hoặc bỏ qua (Tùy nghiệp vụ)
            When(x => x.PaymentMethod != PaymentMethod.QR, () =>
            {
                RuleFor(x => x.BankBin).Empty().WithMessage("Thông tin ngân hàng chỉ áp dụng cho đơn hàng thanh toán QR.");
                RuleFor(x => x.BankAccountNumber).Empty().WithMessage("Thông tin ngân hàng chỉ áp dụng cho đơn hàng thanh toán QR.");
            });

        }
    }
}
