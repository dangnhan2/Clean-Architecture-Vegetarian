using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Dtos.Request;
using Net.payOS.Types;

namespace Vegetarian.Application.Abstractions.Payment
{
    public interface IPaymentGateway
    {
        public Task<PaymentOrderInfoDto> CreatePaymentLink(int amount, int orderCode, List<ItemData> data);
        public Task<string> ConfirmWebHook(WebHookUrlRequestDto request);
        public Task<string> CallBack(HttpRequest request);
    }
}
