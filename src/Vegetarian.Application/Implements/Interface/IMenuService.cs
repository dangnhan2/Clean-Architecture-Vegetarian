using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Dtos.QueryParams;
using Vegetarian.Application.Dtos.Request;
using Vegetarian.Application.Dtos.Response;

namespace Vegetarian.Application.Implements.Interface
{
    public interface IMenuService
    {
        public Task<IEnumerable<MenuSearchDto>> SearchMenuAsync(SearchRequestDto requestDto);
        public Task<PagingResponse<MenuDto>> GetAllMenusAsync(MenuParams menuParams);
        public Task<PagingResponse<MenuDto>> GetAllMenusOnSaleAsync(MenuParams menuParams);
        public Task<IEnumerable<MenuDto>> GetFeaturedMenusAsync();
        public Task<IEnumerable<MenuDto>> GetRelatedMenusAsync(Guid menuId);
        public Task<MenuDto> GetMenuByIdAsync(Guid menuId);
        public Task AddMenuAsync(MenuRequestDto request);
        public Task UpdateMenuAsync(Guid menuId, MenuRequestDto request);
        public Task DeleteMenuAsync(Guid menuId);
    }
}
