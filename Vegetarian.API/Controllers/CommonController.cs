using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Vegetarian.Application.Dtos.Response;
using Vegetarian.Application.Implements.Interface;

namespace Vegetarian.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommonController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CommonController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
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
    }
}
