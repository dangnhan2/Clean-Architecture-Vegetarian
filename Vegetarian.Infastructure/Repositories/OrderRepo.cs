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
    }
}
