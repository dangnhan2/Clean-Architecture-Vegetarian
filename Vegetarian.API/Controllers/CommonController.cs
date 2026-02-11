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
        private readonly IVoucherService _voucherService;

        public CommonController(
            ICategoryService categoryService,
            ICartService cartService,
            IVoucherService voucherService)
        {
            _categoryService = categoryService;
            _cartService = cartService;
            _voucherService = voucherService;
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


        #region voucher endpoints
        [HttpGet("user/vouchers")]
        public async Task<IActionResult> GetAllVoucherByCustomer()
        {
            var result = await _voucherService.GetAllByCustomerAsync();
            var response = ApiResponse<IEnumerable<VoucherDto>>.Success("Lấy dữ liệu thành công", result, StatusCodes.Status200OK);
            return Ok(response);
        }

        [HttpPost("user/voucher/validation")]
        public async Task<IActionResult> ValidateVoucher(ValidationVoucherRequestDto request)
        {
            var result = await _voucherService.ValidateVoucherAsync(request);
            var response = ApiResponse<dynamic>.Success("Áp dụng voucher thành công", result, StatusCodes.Status200OK);
            return Ok(response);
        }
        #endregion
    }
}
