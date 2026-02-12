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
    public class OrderMenuRepo : GenericRepo<OrderMenus>,  IOrderMenuRepo
    {
        public OrderMenuRepo(VegetarianDbContext context) : base(context) { }
    }
}
