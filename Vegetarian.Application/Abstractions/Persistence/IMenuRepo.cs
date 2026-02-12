using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Dtos.Response;
using Vegetarian.Domain.Models;

namespace Vegetarian.Application.Abstractions.Persistence
{
    public interface IMenuRepo : IGenericRepo<Menu>
    {
        public Task<MenuDto?> GetMenuWithCategoryAsync(Guid id);
    }
}
