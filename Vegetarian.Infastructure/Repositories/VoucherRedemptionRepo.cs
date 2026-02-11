using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Repositories;
using Vegetarian.Domain.Models;
using Vegetarian.Infrastructure.Data;

namespace Vegetarian.Infrastructure.Repositories
{
    public class VoucherRedemptionRepo : GenericRepo<VoucherRedemption>, IVoucherRedemptionRepo
    {
        private readonly VegetarianDbContext _context;
        public VoucherRedemptionRepo(VegetarianDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<VoucherRedemption?> GetVoucherRedemptionsByOrderIdAsync(Guid orderId)
        {
            return await _context.VoucherRedemption
                .Include(vr => vr.Voucher)
                .FirstOrDefaultAsync(vr => vr.OrderID == orderId);
        }

        public async Task<int> TodayCountAsync(Guid userId, Guid voucherId)
        {
            return await _context.VoucherRedemption.CountAsync(
                v => v.UserID == userId
                && v.VoucherID == voucherId
                && v.RedeemedAt.Date == DateTimeOffset.UtcNow.Date);
        }
    }
}
