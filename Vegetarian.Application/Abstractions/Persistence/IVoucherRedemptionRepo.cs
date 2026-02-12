using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Domain.Models;

namespace Vegetarian.Application.Abstractions.Persistence
{
    public interface IVoucherRedemptionRepo : IGenericRepo<VoucherRedemption>
    {
        public Task<int> TodayCountAsync(Guid userId, Guid voucherId);
        public Task<VoucherRedemption?> GetVoucherRedemptionsByOrderIdAsync(Guid orderId);
    }
}
