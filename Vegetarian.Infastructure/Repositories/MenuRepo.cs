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
    public class MenuRepo : GenericRepo<Menu>, IMenuRepo
    {
        private readonly VegetarianDbContext _context;
        public MenuRepo(VegetarianDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<MenuDto?> GetMenuWithCategoryAsync(Guid id)
        {
            var menuDto = from m in _context.Menu
                          join c in _context.Category on m.CategoryId equals c.Id into mc
                          from c in mc.DefaultIfEmpty()
                          where m.Id == id
                          select new MenuDto
                          {
                              Id = m.Id,
                              Name = m.Name,
                              Category = c.Name,
                              Description = m.Description,
                              OriginalPrice = m.OriginalPrice,
                              DiscountPrice = m.DiscountPrice,
                              AverageRating = m.AverageRating,
                              ImageUrl = m.ImageUrl,
                              CreatedAt = m.CreatedAt,
                              SoldQuantity = m.SoldQuantity,
                              RatingCount = m.Ratings.Count(),
                              IsAvailable = m.IsAvailable,
                              IsOnSale = m.IsOnSale,
                              DiscountPercent = m.IsOnSale && m.DiscountPrice.HasValue ? (int)(((m.OriginalPrice - m.DiscountPrice) / m.OriginalPrice) * 100) : 0
                          };

            return await menuDto.FirstOrDefaultAsync();
        }
    }
}
