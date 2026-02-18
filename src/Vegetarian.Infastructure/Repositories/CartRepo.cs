using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Abstractions.Persistence;
using Vegetarian.Domain.Models;
using Vegetarian.Infrastructure.Data;

namespace Vegetarian.Infrastructure.Repositories
{
    public class CartRepo : GenericRepo<Cart>, ICartRepo
    {
        private readonly VegetarianDbContext _context;
        public CartRepo(VegetarianDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Cart?> GetCartByCustomerAsync(Guid userId)
        {
            return await _context.Cart
                .Include(c => c.CartItems)
                .ThenInclude(ct => ct.Menu)
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }
    }
}
