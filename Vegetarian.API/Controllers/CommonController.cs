using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Vegetarian.Application.Dtos.Request;
using Vegetarian.Application.Dtos.Response;
using Vegetarian.Application.Implements.Interface;

namespace Vegetarian.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommonController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        private readonly ICartService _cartService;

        public CommonController(
            ICategoryService categoryService,
            ICartService cartService)
        {
            _categoryService = categoryService;
            _cartService = cartService;
        }

        #region category endpoints
        [HttpGet("categories")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllCategories()
        {
            var result = await _categoryService.GetAllAsync();
            var response = ApiResponse<IEnumerable<CategoryDto>>.Success("Lấy dữ liệu thành công", result, StatusCodes.Status200OK);
            return Ok(response);
        }
        #endregion


        #region cart endpoints
        [HttpGet("cart")]
        public async Task<IActionResult> GetCartByCustomer(Guid id)
        {
            var result = await _cartService.GetCartByCustomer(id);
            var response = ApiResponse<CartDto>.Success("Lấy dữ liệu thành công", result, StatusCodes.Status200OK);
            return CreatedAtAction(null, response);
        }

        [HttpPost("cart")]
        public async Task<IActionResult> AddToCart([FromBody] CartRequestDto request)
        {
            await _cartService.AddToCartAsync(request);
            var response = ApiResponse<string>.Success("Thêm item thành công", "", StatusCodes.Status201Created);
            return CreatedAtAction(null, response);
        }
        #endregion
    }
}
