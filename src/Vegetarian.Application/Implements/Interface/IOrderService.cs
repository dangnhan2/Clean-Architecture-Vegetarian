using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Dtos.QueryParams;
using Vegetarian.Application.Dtos.Request;
using Vegetarian.Application.Dtos.Response;

namespace Vegetarian.Application.Implements.Interface
{
    public interface IOrderService
    {
        public Task<PaymentOrderInfoDto> CreateOrderByQRAsync(OrderRequestDto request);
        public Task<int> CreateOrderByCODAsync(OrderRequestDto request);
        public Task<PagingResponse<OrderDto>> GetAllAsync(OrderParams orderParams);
        public Task<PagingResponse<OrderDto>> GetAllAsyncByCustomer(Guid userId, OrderParams orderParams);
        public Task ConfirmPaidOrderAsync(Guid orderId);
    }
}
