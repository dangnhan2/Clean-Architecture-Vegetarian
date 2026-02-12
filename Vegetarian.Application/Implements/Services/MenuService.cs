using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Abstractions.Caching;
using Vegetarian.Application.Abstractions.Storage;
using Vegetarian.Application.Contants;
using Vegetarian.Application.Dtos.QueryParams;
using Vegetarian.Application.Dtos.Request;
using Vegetarian.Application.Dtos.Response;
using Vegetarian.Application.Helper;
using Vegetarian.Application.Implements.Interface;
using Vegetarian.Application.Validator;
using Vegetarian.Domain.Models;

namespace Vegetarian.Application.Implements.Services
{
    public class MenuService : IMenuService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICachingProvider _cacheService;
        private const string folder = "Thumbnail";
        private readonly ICloudinaryStorage _cloudinaryService;

        public MenuService(IUnitOfWork unitOfWork, ICachingProvider cacheService, ICloudinaryStorage cloudinaryService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<PagingResponse<MenuDto>> GetAllMenusAsync(MenuParams menuParams)
        {
            var menus = _unitOfWork.Menu.GetAll();

            if (!string.IsNullOrEmpty(menuParams.Search))
                menus = menus.Where(m => EF.Functions.ILike(EF.Functions.Unaccent(m.Name), "%" + EF.Functions.Unaccent(menuParams.Search) + "%")
                || m.Category.Name.Trim().ToLower().Contains(menuParams.Search.Trim().ToLower()));

            if (menuParams.From.HasValue && menuParams.To.HasValue)
                menus = menus.Where(m =>
                (m.IsOnSale ? m.DiscountPrice : m.OriginalPrice) >= menuParams.From.Value
                &&
                (m.IsOnSale ? m.DiscountPrice : m.OriginalPrice) <= menuParams.To.Value);

            //sort
            if (!string.IsNullOrEmpty(menuParams.SortBy))
            {
                var sortBy = menuParams.SortBy.ToLower();
                var sortOrder = menuParams.SortOrder?.ToLower() ?? "asc";

                menus = sortBy switch
                {
                    "price" => sortOrder == "desc"
                    ? menus.OrderByDescending(m => m.IsOnSale ? m.DiscountPrice : m.OriginalPrice)
                    : menus.OrderBy(m => m.IsOnSale ? m.DiscountPrice : m.OriginalPrice),

                    "soldquantity" => sortOrder == "desc"
                    ? menus.OrderByDescending(m => m.SoldQuantity)
                    : menus.OrderBy(m => m.SoldQuantity),

                    _ => sortOrder == "desc"
                    ? menus.OrderByDescending(m => m.CreatedAt)
                    : menus.OrderBy(m => m.CreatedAt)
                };
            }

            var menusToDTO = menus
                .OrderByDescending(m => m.UpdatedAt)
                .Select(m => new MenuDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    Category = m.Category.Name,
                    Description = m.Description,
                    OriginalPrice = m.OriginalPrice,
                    AverageRating = m.AverageRating,
                    DiscountPrice = m.DiscountPrice,
                    ImageUrl = m.ImageUrl,
                    SoldQuantity = m.SoldQuantity,
                    RatingCount = m.Ratings.Count(),
                    IsAvailable = m.IsAvailable,
                    IsOnSale = m.IsOnSale,
                    CreatedAt = m.CreatedAt,
                    DiscountPercent = m.IsOnSale && m.DiscountPrice.HasValue ? (int)(((m.OriginalPrice - m.DiscountPrice) / m.OriginalPrice) * 100) : 0
                })
                .AsNoTracking();

            if (menuParams.Page != 0 && menuParams.PageSize != 0)
                menusToDTO = menusToDTO.Paging(menuParams.Page, menuParams.PageSize);

            var response = new PagingResponse<MenuDto>(menuParams.Page, menuParams.PageSize, menus.Count(), await menusToDTO.ToArrayAsync());
            return response;
        }

        public async Task<PagingResponse<MenuDto>> GetAllMenusOnSaleAsync(MenuParams menuParams)
        {
            var menus = _unitOfWork.Menu
                .GetAll()
                .Where(m => m.IsAvailable && m.IsOnSale);

            if (!string.IsNullOrEmpty(menuParams.Search))
                menus = menus.Where(m => EF.Functions.ILike(EF.Functions.Unaccent(m.Name), "%" + EF.Functions.Unaccent(menuParams.Search) + "%")
                || m.Category.Name.Trim().ToLower().Contains(menuParams.Search.Trim().ToLower()));

            if (!string.IsNullOrEmpty(menuParams.SortBy))
            {
                var sortBy = menuParams.SortBy.ToLower();
                var sortOrder = menuParams.SortOrder?.ToLower() ?? "asc";

                menus = sortBy switch
                {
                    "price" => sortOrder == "desc"
                    ? menus.OrderByDescending(m => m.IsOnSale ? m.DiscountPrice : m.OriginalPrice)
                    : menus.OrderBy(m => m.IsOnSale ? m.DiscountPrice : m.OriginalPrice),

                    "soldquantity" => sortOrder == "desc"
                    ? menus.OrderByDescending(m => m.SoldQuantity)
                    : menus.OrderBy(m => m.SoldQuantity),

                    _ => sortOrder == "desc"
                    ? menus.OrderByDescending(m => m.CreatedAt)
                    : menus.OrderBy(m => m.CreatedAt)
                };
            }

            var menusToDTO = menus.Select(m => new MenuDto
            {
                Id = m.Id,
                Name = m.Name,
                Category = m.Category.Name,
                Description = m.Description,
                OriginalPrice = m.OriginalPrice,
                AverageRating = m.AverageRating,
                DiscountPrice = m.DiscountPrice,
                ImageUrl = m.ImageUrl,
                SoldQuantity = m.SoldQuantity,
                RatingCount = m.Ratings.Count(),
                IsAvailable = m.IsAvailable,
                IsOnSale = m.IsOnSale,
                CreatedAt = m.CreatedAt,
                DiscountPercent = m.IsOnSale && m.DiscountPrice.HasValue ? (int)(((m.OriginalPrice - m.DiscountPrice) / m.OriginalPrice) * 100) : 0
            }
            )
                .AsNoTracking();

            if (menuParams.Page != 0 && menuParams.PageSize != 0)
                menusToDTO = menusToDTO.Paging(menuParams.Page, menuParams.PageSize);

            var response = new PagingResponse<MenuDto>(menuParams.Page, menuParams.PageSize, menus.Count(),
                await menusToDTO.ToListAsync());

            return response;
        }

        public async Task<IEnumerable<MenuDto>> GetFeaturedMenusAsync()
        {
            var menusToDto = await _unitOfWork.Menu
                 .GetAll()
                 .Where(m => m.IsAvailable)
                 .OrderByDescending(x => x.SoldQuantity)
                 .Take(10)
                 .Select(m => new MenuDto
                 {
                     Id = m.Id,
                     Name = m.Name,
                     Category = m.Category.Name,
                     Description = m.Description,
                     OriginalPrice = m.OriginalPrice,
                     AverageRating = m.AverageRating,
                     DiscountPrice = m.DiscountPrice,
                     ImageUrl = m.ImageUrl,
                     SoldQuantity = m.SoldQuantity,
                     RatingCount = m.Ratings.Count(),
                     IsAvailable = m.IsAvailable,
                     IsOnSale = m.IsOnSale,
                     CreatedAt = m.CreatedAt,
                     DiscountPercent = m.IsOnSale && m.DiscountPrice.HasValue
                       ? (int)(((m.OriginalPrice - m.DiscountPrice.Value) / m.OriginalPrice) * 100)
                      : 0
                 })
                .AsNoTracking()
                .ToListAsync();

            return menusToDto;
        }

        public async Task<IEnumerable<MenuDto>> GetRelatedMenusAsync(Guid menuId)
        {
            var currentMenu = await _unitOfWork.Menu.GetByIdAsync(menuId);

            if (currentMenu == null)
                return Enumerable.Empty<MenuDto>();

            var menus = _unitOfWork.Menu
                .GetAll()
                .Where(m =>
                      m.Id != currentMenu.Id
                   && m.CategoryId == currentMenu.CategoryId
                   && m.IsAvailable)
                .Take(10);

            var menusToDto = await menus
                .Select(m => new MenuDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    Category = m.Category.Name,
                    Description = m.Description,
                    OriginalPrice = m.OriginalPrice,
                    DiscountPrice = m.DiscountPrice,
                    AverageRating = m.AverageRating,
                    ImageUrl = m.ImageUrl,
                    SoldQuantity = m.SoldQuantity,
                    RatingCount = m.Ratings.Count(),
                    IsAvailable = m.IsAvailable,
                    IsOnSale = m.IsOnSale,
                    CreatedAt = m.CreatedAt,
                    DiscountPercent = m.IsOnSale && m.DiscountPrice.HasValue ? (int)(((m.OriginalPrice - m.DiscountPrice) / m.OriginalPrice) * 100) : 0
                })
                .AsNoTracking()
                .ToListAsync();
            return menusToDto;
        }

        public async Task<MenuDto> GetMenuByIdAsync(Guid menuId)
        {
            string cacheKey = CacheKeys.MenuDetail(menuId);
            var cacheMenu = await _cacheService
                .GetAsync<MenuDto>(cacheKey);

            if (cacheMenu != null)
                return cacheMenu;

            var menu = await _unitOfWork.Menu
                .GetMenuWithCategoryAsync(menuId);

            if (menu == null) throw new KeyNotFoundException("Món ăn không tồn tại");

            await _cacheService.SetAsync(cacheKey, menu, TimeSpan.FromMinutes(10));

            return menu;
        }

        public async Task AddMenuAsync(MenuRequestDto request)
        {
            var result = await new MenuValidator().ValidateAsync(request);

            if (!result.IsValid) throw new ValidationDictionaryException(result.ToDictionary());

            var isMenuExist = _unitOfWork.Menu
                .GetAll()
                .Any(m => m.Name.Trim().ToLower() == request.Name.Trim().ToLower());

            if (isMenuExist) throw new DuplicateNameException($"Menu {request.Name} đã tồn tại");

            if (request.IsOnSale && (request.DiscountPrice == null || request.DiscountPrice == 0)) throw new ArgumentException("Món ăn đang có trạng thái giảm giá nhưng chưa cập nhật giá khuyến mãi. Hãy cập nhập giá khuyến mãi");

            var menu = await MappingMenu(request);

            await _unitOfWork.Menu.AddAsync(menu);
            await _unitOfWork.SaveChangeAsync();
        }

        public async Task UpdateMenuAsync(Guid menuId, MenuRequestDto request)
        {
            var result = await new MenuValidator().ValidateAsync(request);

            if (!result.IsValid) throw new ValidationDictionaryException(result.ToDictionary());

            var menu = await _unitOfWork.Menu
                .GetByIdAsync(menuId);

            if (menu == null) throw new KeyNotFoundException("Món ăn không tồn tại");

            var isMenuExist = _unitOfWork.Menu
                .GetAll().Any(m => m.Name.Replace(" ", "") == request.Name.Replace(" ", "") && m.Id != menuId);

            if (isMenuExist) throw new DuplicateNameException($"Menu {request.Name} đã tồn tại");

            if (request.IsOnSale && (request.DiscountPrice == null || request.DiscountPrice == 0)) throw new ArgumentException("Món ăn đang có trạng thái giảm giá nhưng chưa cập nhật giá giảm. Hãy cập nhập giá giảm");

            menu.Name = request.Name;
            menu.CategoryId = request.CategoryId;
            menu.OriginalPrice = request.OriginalPrice;
            menu.DiscountPrice = request.DiscountPrice;
            menu.IsAvailable = request.IsAvailable;
            menu.IsOnSale = request.IsOnSale;
            menu.Description = request.Description;
            menu.UpdatedAt = DateTime.UtcNow;

            if (request.Thumbnail != null)
            {
                var url = await _cloudinaryService.UploadImage(request.Thumbnail, folder);
                await _cloudinaryService.DeleteImage(menu.ImageUrl);
                menu.ImageUrl = url;
            }

            _unitOfWork.Menu.Update(menu);
            await _unitOfWork.SaveChangeAsync();
            await _cacheService.RemoveAsync(CacheKeys.MenuDetail(menuId));
        }

        public async Task DeleteMenuAsync(Guid menuId)
        {
            var menu = await _unitOfWork.Menu.GetByIdAsync(menuId);

            if (menu == null) throw new KeyNotFoundException("Món ăn không tồn tại");

            if (menu.IsOnSale || menu.IsAvailable) throw new InvalidOperationException("Món ăn đang được bán, hãy cập nhật lại trạng thái trước khi xóa");

            _unitOfWork.Menu.Remove(menu);
            await _unitOfWork.SaveChangeAsync();
        }

        public async Task<IEnumerable<MenuSearchDto>> SearchMenuAsync(SearchRequestDto requestDto)
        {
            var menus = _unitOfWork.Menu
                .GetAll()
                .Where(m => EF.Functions.ILike(EF.Functions.Unaccent(m.Name), "%" + EF.Functions.Unaccent(requestDto.Keyword) + "%"));

            var menusToDto = menus.Select(m => new MenuSearchDto
            {
                Id = m.Id,
                Name = m.Name,
                Price = m.IsOnSale ? m.DiscountPrice : m.OriginalPrice,
                ImageUrl = m.ImageUrl
            }).Take(5);

            return await menusToDto.ToListAsync();
        }


        #region helper method
        private async Task<Menu> MappingMenu(MenuRequestDto request)
        {
            var menu = new Menu
            {
                Name = request.Name,
                CategoryId = request.CategoryId,
                OriginalPrice = request.OriginalPrice,
                DiscountPrice = request.DiscountPrice,
                IsOnSale = request.IsOnSale,
                IsAvailable = request.IsAvailable,
                SoldQuantity = 0,
                Description = request.Description
            };

            if (request.Thumbnail != null)
            {
                var url = await _cloudinaryService.UploadImage(request.Thumbnail, folder);
                menu.ImageUrl = url;
            }

            return menu;
        }      
        #endregion
    }
}
