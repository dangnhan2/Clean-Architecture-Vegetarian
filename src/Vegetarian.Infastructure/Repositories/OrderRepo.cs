using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Abstractions.Persistence;
using Vegetarian.Domain.Models;
using Vegetarian.Infrastructure.Data;

namespace Vegetarian.Infrastructure.Repositories
{
    public class OrderRepo : GenericRepo<Order>, IOrderRepo
    {
        private readonly VegetarianDbContext _context;
        public OrderRepo(VegetarianDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Order?> GetOrderByOrderCode(int code)
        {
            return await _context.Order
                .Include(o => o.OrderMenus)
                .FirstOrDefaultAsync(o => o.OrderCode == code);
        }

        public async Task DecreasedVoucher(Guid voucherId)
        {
            var rowAffected = await _context.Database.ExecuteSqlInterpolatedAsync($@"
               UPDATE Voucher
               SET UsedCount = UsedCount + 1
               Where Id = {voucherId} AND UsedCount < UsageLimit
            ");
        }
    }
}
