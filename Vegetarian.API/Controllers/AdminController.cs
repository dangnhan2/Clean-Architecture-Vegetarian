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

        public AdminController(
            ICategoryService categoryService,
            IVoucherService voucherService)
        {
            _categoryService = categoryService;
            _voucherService = voucherService;
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
    }
}
