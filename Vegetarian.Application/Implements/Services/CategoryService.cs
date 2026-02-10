using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Contants;
using Vegetarian.Application.Dtos.Request;
using Vegetarian.Application.Dtos.Response;
using Vegetarian.Application.Implements.Caching;
using Vegetarian.Application.Implements.Interface;
using Vegetarian.Application.Validator;
using Vegetarian.Domain.Models;

namespace Vegetarian.Application.Implements.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICachingService _cacheService;

        public CategoryService(IUnitOfWork unitOfWork, ICachingService cacheService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
        }

        public async Task AddAsync(CategoryRequestDto request)
        {
            var result = await new CategoryValidator().ValidateAsync(request);

            if (!result.IsValid)
                throw new ValidationDictionaryException(result.ToDictionary());

            var isCategoryExist = _unitOfWork.Category
                .GetAll()
                .Any(c => c.Name.Contains(request.Name));

            if (isCategoryExist)
                throw new DuplicateNameException($"{request.Name} đã tồn tại");

            var newCategory = MappingCategory(request);

            await _unitOfWork.Category.AddAsync(newCategory);
            await _unitOfWork.SaveChangeAsync();

            await _cacheService.RemoveAsync(CacheKeys.CATEGORIES_PREFIX);
        }

        public async Task DeleteAsync(Guid categoryId)
        {
            var category = await _unitOfWork.Category.GetByIdAsync(categoryId);

            if (category == null)
                throw new KeyNotFoundException("Không tìm thấy danh mục");

            _unitOfWork.Category.Remove(category);
            await _unitOfWork.SaveChangeAsync();

            await _cacheService.RemoveAsync(CacheKeys.CATEGORIES_PREFIX);
        }

        public async Task<IEnumerable<CategoryDto>> GetAllAsync()
        {
            string cacheKey = CacheKeys.CATEGORIES_PREFIX;
            var cachedCategories = await _cacheService.GetAsync<IEnumerable<CategoryDto>>(cacheKey);

            if (cachedCategories != null)
                return cachedCategories;

            var categories = _unitOfWork.Category.GetAll();

            var categoriesToDTO = await categories
                .OrderBy(c => c.Name)
                .AsNoTracking()
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .ToListAsync();

            await _cacheService.SetAsync(cacheKey, categoriesToDTO, TimeSpan.FromMinutes(30));

            return categoriesToDTO;
        }

        public async Task UpdateAsync(Guid categoryId, CategoryRequestDto request)
        {
            var result = await new CategoryValidator().ValidateAsync(request);

            if (!result.IsValid)
                throw new ValidationDictionaryException(result.ToDictionary());

            var category = await _unitOfWork.Category.GetByIdAsync(categoryId);

            if (category == null)
                throw new KeyNotFoundException("Không tìm thấy danh mục");

            var isCategoryExist = _unitOfWork.Category
                .GetAll()
                .Any(c => c.Name.Contains(request.Name) && c.Id != categoryId);

            if (isCategoryExist)
                throw new DuplicateNameException($"{request.Name} đã tồn tại");

            category.Name = request.Name;

            _unitOfWork.Category.Update(category);
            await _unitOfWork.SaveChangeAsync();

            await _cacheService.RemoveAsync(CacheKeys.CATEGORIES_PREFIX);
        }


        #region helper method
        private Category MappingCategory(CategoryRequestDto request)
        {
            Category category = new Category
            {
                Name = request.Name,
            };

            return category;
        }
        #endregion
    }
}
