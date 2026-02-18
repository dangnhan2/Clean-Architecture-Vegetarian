using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Dtos.Request;
using Vegetarian.Application.Dtos.Response;

namespace Vegetarian.Application.Implements.Interface
{
    public interface ICategoryService
    {
        public Task<IEnumerable<CategoryDto>> GetAllAsync();
        public Task AddAsync(CategoryRequestDto request);
        public Task UpdateAsync(Guid categoryId, CategoryRequestDto request);
        public Task DeleteAsync(Guid categoryId);
    }
}
