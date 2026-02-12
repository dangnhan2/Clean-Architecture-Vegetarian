using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Domain.Models;

namespace Vegetarian.Application.Abstractions.Persistence
{
    public interface IOrderRepo : IGenericRepo<Order>
    {
        public Task<Order?> GetOrderByOrderCode(int code);
    }
}
