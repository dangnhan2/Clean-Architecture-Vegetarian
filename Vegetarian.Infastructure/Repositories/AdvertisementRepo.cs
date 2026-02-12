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
    public class AdvertisementRepo : GenericRepo<Advertisement>, IAdvertisementRepo
    {
        public AdvertisementRepo(VegetarianDbContext context) : base(context) { }
    }
}
