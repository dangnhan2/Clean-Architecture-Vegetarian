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
            var menuDto = _context.Menu.Join(
                          _context.Category,
                          menu => menu.CategoryId,
                          category => category.Id,
                          (menu, category) => new MenuDto
                          {
                              Id = menu.Id,
                              Name = menu.Name,
                              Category = menu.Name,
                              Description = menu.Description,
                              OriginalPrice = menu.OriginalPrice,
                              DiscountPrice = menu.DiscountPrice,
                              AverageRating = menu.AverageRating,
                              ImageUrl = menu.ImageUrl,
                              CreatedAt = menu.CreatedAt,
                              SoldQuantity = menu.SoldQuantity,
                              RatingCount = menu.Ratings.Count(),
                              IsAvailable = menu.IsAvailable,
                              IsOnSale = menu.IsOnSale,
                              DiscountPercent = menu.IsOnSale && menu.DiscountPrice.HasValue ? (int)(((menu.OriginalPrice - menu.DiscountPrice) / menu.OriginalPrice) * 100) : 0
                          });

            return await menuDto.FirstOrDefaultAsync();
        }
    }
}
