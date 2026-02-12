using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Abstractions.Persistence;
using Vegetarian.Application.Dtos.Response;
using Vegetarian.Domain.Models;
using Vegetarian.Infrastructure.Data;

namespace Vegetarian.Infrastructure.Repositories
{
    public class UserRepo : GenericRepo<User>, IUserRepo
    {
        private readonly VegetarianDbContext _context;
        public UserRepo(VegetarianDbContext context) : base(context) {
            _context = context;
        }

        public async Task<IEnumerable<TopBuyerDto>> GetTop5BuyersMonthly()
        {
            var month = DateTimeOffset.UtcNow.Month;

            var topBuyers = await _context.Order
                            .Where(o => o.OrderDate.Month == month)
                            .GroupBy(o => o.User)
                            .Select(g => new TopBuyerDto
                            {
                                Id = g.Key.Id,
                                Email = g.Key.Email,
                                PhoneNumber = g.Key.PhoneNumber,
                                UserName = g.Key.UserName,
                                TotalAmountInAMonth = g.Sum(o => o.TotalAmount)
                            })
                            .OrderByDescending(o => o.TotalAmountInAMonth)
                            .Take(5)
                            .ToListAsync();

            return topBuyers;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users.Include(u => u.EmailOtps).FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetUserContainsCartAsync(Guid id)
        {
            return await _context.Users.Include(u => u.Cart).FirstOrDefaultAsync(u => u.Id == id);
        }
    }
}
