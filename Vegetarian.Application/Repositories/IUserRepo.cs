using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Dtos.Response;
using Vegetarian.Domain.Models;

namespace Vegetarian.Application.Repositories
{
    public interface IUserRepo : IGenericRepo<User>
    {
        public Task<User?> GetUserByEmailAsync(string email);
        public Task<User?> GetUserContainsCartAsync(Guid id);
        public Task<IEnumerable<TopBuyerDto>> GetTop5BuyersMonthly();
    }
}
