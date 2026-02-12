using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Vegetarian.Application.Dtos.QueryParams;
using Vegetarian.Application.Dtos.Request;
using Vegetarian.Application.Dtos.Response;
using Vegetarian.Application.Implements.Interface;

namespace Vegetarian.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        private readonly IVoucherService _voucherService;
        private readonly IOrderService _orderService;
        private readonly IMenuService _menuService;
        private readonly IUserService _userService;

        public AdminController(
            ICategoryService categoryService,
            IVoucherService voucherService,
            IOrderService orderService,
            IMenuService menuService,
            IUserService userService)
        {
            _categoryService = categoryService;
            _voucherService = voucherService;
            _orderService = orderService;
            _menuService = menuService;
            _userService = userService;
        }

        #region category endpoints
        [HttpGet("categories")]
        public async Task<IActionResult> GetAllCategories()
        {
            var result = await _categoryService.GetAllAsync();
            var response = ApiResponse<IEnumerable<CategoryDto>>.Success("Lấy dữ liệu thành công", result, StatusCodes.Status200OK);
            return Ok(response);
        }

        [HttpPost("category")]
        public async Task<IActionResult> CreateCategory(CategoryRequestDto request)
        {
            await _categoryService.AddAsync(request);
            var response = ApiResponse<dynamic>.Success("Thêm mới thành công", null, StatusCodes.Status201Created);
            return CreatedAtAction(nameof(GetAllCategories), response);
        }

        [HttpPut("category/{id}")]
        public async Task<IActionResult> UpdateCatetogy(Guid id, CategoryRequestDto request)
        {
            await _categoryService.UpdateAsync(id, request);
            var response = ApiResponse<dynamic>.Success("Cập nhật thành công", null, StatusCodes.Status200OK);
            return Ok(response);

        }

        [HttpDelete("category/{id}")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            await _categoryService.DeleteAsync(id);
            var response = ApiResponse<dynamic>.Success("Xóa thành công", null, StatusCodes.Status200OK);
            return Ok(response);
        }
        #endregion


        #region  voucher endpoints
        [HttpGet("vouchers")]
        public async Task<IActionResult> GetVoucher([FromQuery] VoucherParams voucherParams)
        {
            var result = await _voucherService.GetAllByAdminAsync(voucherParams);
            var response = ApiResponse<PagingResponse<VoucherDto>>.Success("Lấy dữ liệu thành công", result, StatusCodes.Status200OK);
            return Ok(response);
        }

        [HttpPost("voucher")]
        public async Task<IActionResult> CreateVoucher([FromBody] VoucherRequestDto request)
        {
            await _voucherService.AddAsync(request);
            var response = ApiResponse<string>.Success("Thêm mới thành công", "", StatusCodes.Status201Created);
            return CreatedAtAction(nameof(GetVoucher), response);
        }

        [HttpPut("voucher/{id}")]
        public async Task<IActionResult> UpdateVoucher(Guid id, [FromBody] VoucherRequestDto request)
        {
            await _voucherService.UpdateAsync(id, request);
            var response = ApiResponse<string>.Success("Cập nhật thành công", "", StatusCodes.Status200OK);
            return Ok(response);
        }

        [HttpDelete("voucher/{id}")]
        public async Task<IActionResult> DeleteVoucher(Guid id)
        {
            await _voucherService.DeleteAsync(id);
            var response = ApiResponse<string>.Success("Xóa thành công", "", StatusCodes.Status200OK);
            return Ok(response);
        }
        #endregion


        #region order endpoints
        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders([FromQuery] OrderParams orderParams)
        {
            var result = await _orderService.GetAllAsync(orderParams);
            var response = ApiResponse<PagingResponse<OrderDto>>.Success("Lấy dữ liệu thành công", result, StatusCodes.Status200OK);
            return Ok(response);
        }
        #endregion


        #region menu endpoints
        [HttpGet("menus")]
        public async Task<IActionResult> GetMenus([FromQuery] MenuParams menuParams)
        {
            var result = await _menuService.GetAllMenusAsync(menuParams);
            var response = ApiResponse<PagingResponse<MenuDto>>.Success("Lấy dữ liệu thành công", result, StatusCodes.Status200OK);
            return Ok(response);
        }

        [HttpPost("menu")]
        public async Task<IActionResult> AddMenu([FromForm] MenuRequestDto request)
        {
            await _menuService.AddMenuAsync(request);
            var response = ApiResponse<dynamic>.Success("Thêm mới thành công", "", StatusCodes.Status201Created);
            return CreatedAtAction(nameof(GetMenus), response);
        }

        [HttpPut("menu/{id}")]
        public async Task<IActionResult> UpdateMenu(Guid id, [FromForm] MenuRequestDto request)
        {
            await _menuService.UpdateMenuAsync(id, request);
            var response = ApiResponse<dynamic>.Success("Cập nhật thành công", "", StatusCodes.Status200OK);
            return Ok(response);
        }

        [HttpDelete("menu/{id}")]
        public async Task<IActionResult> DeleteMenu(Guid id)
        {
            await _menuService.DeleteMenuAsync(id);
            var response = ApiResponse<dynamic>.Success("Xóa thành công", "", StatusCodes.Status200OK);
            return Ok(response);
        }
        #endregion


        #region user endpoints
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] UserParams userParams)
        {
            var result = await _userService.GetAllAsync(userParams);
            var response = ApiResponse<PagingResponse<UserDto>>.Success("Lấy dữ liệu thành công", result, StatusCodes.Status200OK);
            return Ok(response);
        }

        [HttpPut("user_banning/{id}")]
        public async Task<IActionResult> BanUser(Guid id)
        {
            await _userService.BanUserAsync(id);

            var response = ApiResponse<dynamic>.Success("Cập nhật người dùng thành công", "", StatusCodes.Status200OK);

            return Ok(response);
        }

        [HttpPut("user_unbanning/{id}")]
        public async Task<IActionResult> UnbanUser(Guid id)
        {
            await _userService.UnBanUserAsync(id);
            var response = ApiResponse<dynamic>.Success("Cập nhật người dùng thành công", "", StatusCodes.Status200OK);

            return Ok(response);
        }
        #endregion
    }
}
