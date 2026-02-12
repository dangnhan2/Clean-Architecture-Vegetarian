using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Net.payOS;
using Net.payOS.Types;
using Newtonsoft.Json.Linq;
using RedLockNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application;
using Vegetarian.Application.Abstractions.Notifications;
using Vegetarian.Application.Abstractions.Payment;
using Vegetarian.Application.Dtos.Request;
using Vegetarian.Domain.Enum;
using Vegetarian.Infrastructure.Options;

namespace Vegetarian.Infrastructure.Services.PayOs
{
    public class PaymentGateway : IPaymentGateway
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly PayOS _payOS;
        private readonly INotificationSender _notificationSenderServer;
        private readonly PayOsOptions _options;
        private readonly IDistributedLockFactory _redLockFactory;


        public PaymentGateway(
            IUnitOfWork unitOfWork,
            INotificationSender notificationSenderServer,
            PayOS payOS,
            IOptions<PayOsOptions> options,
            IDistributedLockFactory redLockFactory
            )
        {
            _unitOfWork = unitOfWork;
            _payOS = payOS;
            _options = options.Value;
            _notificationSenderServer = notificationSenderServer;
            _redLockFactory = redLockFactory;
        }

        public async Task<string> CallBack(HttpRequest request)
        {
            using var reader = new StreamReader(request.Body, Encoding.UTF8);
            var rawJson = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(rawJson))
                throw new ArgumentNullException("Empty body");

            // Parse JSON
            var root = JObject.Parse(rawJson);
            var signatureProvided = root["signature"]?.ToString();
            var data = root["data"] as JObject;

            if (string.IsNullOrEmpty(signatureProvided) || data == null)
                throw new ArgumentNullException("Invalid payload");

            // Build transactionStr = key=value&key2=value2...
            var sorted = data.Properties().OrderBy(p => p.Name, StringComparer.Ordinal).ToList();

            var sb = new StringBuilder();

            for (int i = 0; i < sorted.Count; i++)
            {
                var prop = sorted[i];
                sb.Append(prop.Name).Append('=').Append(prop.Value.ToString());
                if (i < sorted.Count - 1) sb.Append('&');
            }
            var transactionStr = sb.ToString();

            // Compute HMAC SHA256
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.ChecksumKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(transactionStr));
            var signatureComputed = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

            if (!string.Equals(signatureProvided, signatureComputed, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Invalid signature");

            // check order if success 
            var code = data["orderCode"]?.ToObject<int>() ?? throw new InvalidDataException("Mã đơn hàng không hợp lệ"); ;

            var order = await _unitOfWork.Order.GetOrderByOrderCode(code);
            if (order == null)
                throw new KeyNotFoundException("Không tìm thấy order");


            var voucherRedemption = await _unitOfWork.VoucherRedemption.GetVoucherRedemptionsByOrderIdAsync(order.Id);

            if (voucherRedemption != null)
            {
                using var redLock = await _redLockFactory.CreateLockAsync(
                                  $"lock:voucher:{voucherRedemption.VoucherID}",
                                  TimeSpan.FromSeconds(30));

                if (!redLock.IsAcquired)
                    throw new Exception("Cannot acquire voucher lock");

                await _unitOfWork.BeginTransactionAsync();

                try
                {
                    voucherRedemption.VoucherRedemptionStatus = Domain.Enum.VoucherRedemptionStatus.Used;

                    voucherRedemption.Voucher.ReservedCount--;
                    voucherRedemption.Voucher.UsedCount++;

                    if (voucherRedemption.Voucher.UsedCount >= voucherRedemption.Voucher.UsageLimit)
                        voucherRedemption.Voucher.IsActive = false;

                    // Update status after order is paid
                    order.Status = OrderStatus.Paid;

                    // Update sold quantity of each menu in paid order
                    foreach (var menu in order.OrderMenus)
                    {
                        var item = await _unitOfWork.Menu.GetByIdAsync(menu.MenuId);

                        if (item != null)
                            item.SoldQuantity = item.SoldQuantity + menu.Quantity;
                    }

                    var cart = await _unitOfWork.Cart.GetCartByCustomerAsync(order.UserId);

                    if (cart == null)
                        throw new KeyNotFoundException("Giỏ hàng không tồn tại");

                    _unitOfWork.Cart.Remove(cart);
                    _unitOfWork.Order.Update(order);
                    await _unitOfWork.SaveChangeAsync();

                    await _unitOfWork.CommitTransactionAsync();
                    await _notificationSenderServer.NotifyAdminWhenNewOrderCreatedAsync(order.OrderCode, order.TotalAmount);
                }
                catch
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw;
                }

            }

            return "Webhook processed successfully";
        }

        public async Task<string> ConfirmWebHook(WebHookUrlRequestDto request)
        {
            var result = await _payOS.confirmWebhook(request.Url);
            return result;
        }

        public async Task<PaymentOrderInfoDto> CreatePaymentLink(int amount, int orderCode, List<ItemData> data)
        {
            var returnUrl = _options.Frontend__URI;
            var cancelUrl = _options.Frontend__URI;

            var paymentLinkRequest = new PaymentData(
                 orderCode: orderCode,
                 amount: amount,
                 description: "Thanh toán đơn hàng",
                 items: data,
                 expiredAt: (int)DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds(),
                 returnUrl: $"{returnUrl}checkout/success?orderCode=${orderCode}&paymentMethod=QR",
                 cancelUrl: cancelUrl
            );

            var paymentInfo = await _payOS.createPaymentLink(paymentLinkRequest);

            var response = new PaymentOrderInfoDto
            {
                CheckoutUrl = paymentInfo.checkoutUrl,
                OrderCode = (int)paymentInfo.orderCode
            };

            return response;
        }
    }
}
