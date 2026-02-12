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
    public class RatingRepo : GenericRepo<Rating>, IRatingRepo
    {   
        private readonly VegetarianDbContext _context;
        public RatingRepo(VegetarianDbContext context) : base(context) {
           _context = context;
        }

        public async Task<double> GetAverageRating(Guid menuId)
        {
            var ratings = _context.Rating.Where(r => r.MenuId == menuId);

            if (!await ratings.AnyAsync())
                return 0;

            var avg = await ratings.AverageAsync(r => r.Stars);

            return avg;
        }
    }
}
