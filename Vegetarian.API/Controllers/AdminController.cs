using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        public AdminController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
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
    }
}
