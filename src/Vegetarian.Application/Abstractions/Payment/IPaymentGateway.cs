using Microsoft.AspNetCore.Http;
using PayOS.Models.Webhooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Dtos.Request;

namespace Vegetarian.Application.Abstractions.Payment
{
    public interface IPaymentGateway
    {
        public Task<PaymentOrderInfoDto> CreatePaymentLink(int amount, int orderCode);
        public Task<string> ConfirmWebHook(string webhookUrl);
        public Task<string> Payout(int totalAmount, string accountNumber, string bin);
        public Task<string> CallBack(HttpRequest request);
    }
}
