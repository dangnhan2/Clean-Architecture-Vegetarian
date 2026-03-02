using DotNetEnv;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using PayOS;
using PayOS.Models.V1.Payouts;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using RedLockNet;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Vegetarian.Application;
using Vegetarian.Application.Abstractions.Notifications;
using Vegetarian.Application.Abstractions.Payment;
using Vegetarian.Application.Dtos.Request;
using Vegetarian.Domain.Enum;


namespace Vegetarian.Infrastructure.Services.PaymentGateway
{
    public class PaymentGateway : IPaymentGateway
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly PayOSClient _payOS;
        private readonly INotificationSender _notificationSenderServer;
        private readonly IDistributedLockFactory _redLockFactory;


        public PaymentGateway(
            IUnitOfWork unitOfWork,
            INotificationSender notificationSenderServer,
            PayOSClient payOS,
            IDistributedLockFactory redLockFactory
            )
        {
            _unitOfWork = unitOfWork;
            _payOS = payOS;
            _notificationSenderServer = notificationSenderServer;
            _redLockFactory = redLockFactory;
        }

        public async Task<string> CallBack(HttpRequest request)
        {
            using var reader = new StreamReader(request.Body);
            var body = await reader.ReadToEndAsync();

            var webhook = JsonSerializer.Deserialize<Webhook>(
             body,
             new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            Log.Information("Information of webhook : {0}", webhook);
            if (webhook?.Data == null)
                throw new ArgumentNullException(nameof(webhook), "Invalid payload");

            // Verify signature + parse data (SDK làm)
            var result = await _payOS.Webhooks.VerifyAsync(webhook);

            var orderCode = (int)result.OrderCode;

            var order = await _unitOfWork.Order.GetOrderByOrderCode(orderCode);

            if (order == null)
                throw new KeyNotFoundException("Không tìm thấy order");

            if (order == null)
                throw new KeyNotFoundException("Không tìm thấy order");


            var voucherRedemption = await _unitOfWork.VoucherRedemption.GetVoucherRedemptionsByOrderIdAsync(order.Id);

            var cart = await _unitOfWork.Cart.GetCartByCustomerAsync(order.UserId);

            if (cart == null)
                throw new KeyNotFoundException("Giỏ hàng không tồn tại");

            _unitOfWork.Cart.Remove(cart);

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
                    voucherRedemption.VoucherRedemptionStatus = VoucherRedemptionStatus.Used;

                    voucherRedemption.Voucher.ReservedCount--;
                    voucherRedemption.Voucher.UsedCount++;

                    if (voucherRedemption.Voucher.UsedCount >= voucherRedemption.Voucher.UsageLimit)
                        voucherRedemption.Voucher.IsActive = false;

                    // Update status after order is paid
                    order.Status = OrderStatus.Paid;
                    order.CreatedAt = DateTimeOffset.UtcNow;

                    // Update sold quantity of each menu in paid order
                    foreach (var menu in order.OrderMenus)
                    {
                        var item = await _unitOfWork.Menu.GetByIdAsync(menu.MenuId);

                        if (item != null)
                            item.SoldQuantity = item.SoldQuantity + menu.Quantity;
                    }    

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
            else
            {
                order.Status = OrderStatus.Paid;
                order.CreatedAt = DateTimeOffset.UtcNow;
                _unitOfWork.Order.Update(order);
                await _unitOfWork.SaveChangeAsync();
            }
            
            return "Webhook processed successfully";
        }

        public async Task<string> ConfirmWebHook(string webhookUrl)
        {
            var result = await _payOS.Webhooks.ConfirmAsync(webhookUrl);
            return result.WebhookUrl;
        }

        public async Task<PaymentOrderInfoDto> CreatePaymentLink(int amount, int orderCode)
        {
            Env.Load();
            var url = Env.GetString("Frontend__URI");

            Log.Information(url);

            var paymentLinkRequest = new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = amount,
                Description = "Thanh toán đơn hàng",
                ExpiredAt = (int)DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds(),
                ReturnUrl = $"{url}/checkout/success?orderCode=${orderCode}&paymentMethod=QR",
                CancelUrl = url
            };


            var paymentInfo = await _payOS.PaymentRequests.CreateAsync(paymentLinkRequest);

            var response = new PaymentOrderInfoDto
            {
                CheckoutUrl = paymentInfo.CheckoutUrl,
                OrderCode = (int)paymentInfo.OrderCode
            };

            return response;

        }

        public async Task<string> Payout(int totalAmount, string accountNumber, string bin)
        {
            var payoutRequest = new PayoutRequest
            {
                ReferenceId = "payout",
                Amount = totalAmount,
                Description = "Hoàn tiền đơn hàng",
                ToAccountNumber = accountNumber,
                ToBin = bin
            };

            var response = await _payOS.Payouts.CreateAsync(payoutRequest);

            if (response.Id != null)
            {
                return "Hoàn tiền thành công";
            }
            else
            {
                return "Hoàn tiền thất bại, vui lòng liên hệ với admin";
            }
        }

    }
}
