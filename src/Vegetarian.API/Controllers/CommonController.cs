using Microsoft.AspNetCore.Authorization;
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
    [Authorize (Roles = "Admin, Customer")]
    public class CommonController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        private readonly ICartService _cartService;
        private readonly IVoucherService _voucherService;
        private readonly IOrderService _orderService;
        private readonly IMenuService _menuService;
        private readonly IUserService _userService;
        private readonly IAddressService _addressService;
        private readonly IRatingService _ratingService;
        private readonly IAdvertisementService _advertisementService;

        public CommonController(
            ICategoryService categoryService,
            ICartService cartService,
            IVoucherService voucherService,
            IOrderService orderService,
            IMenuService menuService,
            IUserService userService,
            IAddressService addressService,
            IRatingService ratingService,
            IAdvertisementService advertisementService)
        {
            _categoryService = categoryService;
            _cartService = cartService;
            _voucherService = voucherService;
            _orderService = orderService;
            _menuService = menuService;
            _userService = userService;
            _addressService = addressService;
            _ratingService = ratingService;
            _advertisementService = advertisementService;
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


        #region order enpoints
        [HttpPost("order/qr")]
        public async Task<IActionResult> CreateOrderWithQR([FromBody] OrderRequestDto request)
        {
            var result = await _orderService.CreateOrderByQRAsync(request);
            var response = ApiResponse<dynamic>.Success("Tạo đơn thành công", result, StatusCodes.Status201Created);
            return CreatedAtAction(null, response);
        }

        [HttpPost("order/cod")]
        public async Task<IActionResult> CreateOrderWithCOD([FromBody] OrderRequestDto request)
        {
            var result = await _orderService.CreateOrderByCODAsync(request);
            var response = ApiResponse<int>.Success("Tạo đơn thành công", result, StatusCodes.Status201Created);
            return CreatedAtAction(null, response);
        }
        #endregion


        #region order history endpoint
        [HttpGet("user/{id}/orders")]
        public async Task<IActionResult> GetAllOrderByCustomer(Guid id, [FromQuery] OrderParams orderParams)
        {
            var result = await _orderService.GetAllAsyncByCustomer(id, orderParams);
            var response = ApiResponse<PagingResponse<OrderDto>>.Success("Lấy dữ liệu thành công", result, StatusCodes.Status200OK);
            return Ok(response);
        }
        #endregion


        #region menu endpoints
        [HttpGet("menus")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMenus([FromQuery] MenuParams menuParams)
        {
            var result = await _menuService.GetAllMenusAsync(menuParams);
            var response = ApiResponse<PagingResponse<MenuDto>>.Success("Lấy dữ liệu thành công", result, StatusCodes.Status200OK);
            return Ok(response);
        }

        [HttpGet("menu/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMenuById(Guid id)
        {
            var result = await _menuService.GetMenuByIdAsync(id);
            var response = ApiResponse<MenuDto>.Success("Lấy dữ liệu thành công", result, StatusCodes.Status200OK);
            return Ok(response);
        }

        [HttpGet("menus/featured")]
        [AllowAnonymous]
        public async Task<IActionResult> GetFeaturedMenus()
        {
            var result = await _menuService.GetFeaturedMenusAsync();
            var response = ApiResponse<dynamic>.Success("Lấy dữ liệu thành công", result, StatusCodes.Status200OK);

            return Ok(response);
        }

        [HttpGet("menus/{id}/related")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRelatedMenus(Guid id)
        {
            var result = await _menuService.GetRelatedMenusAsync(id);
            var response = ApiResponse<dynamic>.Success("Lấy dữ liệu thành công", result, StatusCodes.Status200OK);

            return Ok(response);
        }

        [HttpGet("menus/onsale")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMenusOnSale([FromQuery] MenuParams menuParams)
        {
            var result = await _menuService.GetAllMenusOnSaleAsync(menuParams);
            var response = ApiResponse<PagingResponse<MenuDto>>.Success("Lấy dữ liệu thành công", result, StatusCodes.Status200OK);
            return Ok(response);
        }
        #endregion


        #region user's account endpoint
        [HttpPut("user/profile/{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromForm] UserRequestDto request)
        {
            await _userService.UploadProfileAsync(id, request);
            var response = ApiResponse<string>.Success("Cập nhật thành công", "", StatusCodes.Status200OK);
            return Ok(response);
        }

        [HttpGet("user/{id}")]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            var result = await _userService.GetUserByIdAsync(id);

            var response = ApiResponse<UserDto>.Success("Lấy dữ liệu thành công", result, StatusCodes.Status200OK);
            return Ok(response);
        }
        #endregion


        #region user's address endpoints
        [HttpGet("user/{id}/addresses")]
        public async Task<IActionResult> GetAddresses(Guid id)
        {
            var result = await _addressService.GetAllByUserAsync(id);
            var response = ApiResponse<IEnumerable<AddressDto>>.Success("Lấy dữ liệu thành công", result, StatusCodes.Status200OK);

            return Ok(response);
        }

        [HttpPost("address")]
        public async Task<IActionResult> AddAddress([FromBody] AddressRequestDto request)
        {
            await _addressService.AddAsync(request);
            var response = ApiResponse<string>.Success("Thêm mới thành công", null, StatusCodes.Status201Created);
            return CreatedAtAction(null, response);
        }

        [HttpPut("address/{id}")]
        public async Task<IActionResult> UpdateAddress(Guid id, [FromBody] AddressRequestDto request)
        {
            await _addressService.UpdateAsync(id, request);
            var response = ApiResponse<string>.Success("Cập nhật thành công", null, StatusCodes.Status200OK);
            return Ok(response);
        }

        [HttpPut("address/default/{id}")]
        public async Task<IActionResult> SetAddressDefault(Guid id)
        {
            await _addressService.SetAddressAsDefault(id);
            var response = ApiResponse<string>.Success("Cập nhật thành công", null, StatusCodes.Status200OK);
            return Ok(response);
        }

        [HttpDelete("address/{id}")]
        public async Task<IActionResult> DeleteAddress(Guid id)
        {
            await _addressService.DeleteAsync(id);
            var response = ApiResponse<string>.Success("Xóa thành công", null, StatusCodes.Status200OK);
            return Ok(response);
        }
        #endregion


        #region rating endpoints
        [AllowAnonymous]
        [HttpGet("ratings/menu/{id}")]
        public async Task<IActionResult> GetAllRatingsByMenuId(Guid id, [FromQuery] RatingParams ratingParams)
        {

            var result = await _ratingService.GetAllRatingsByMenuAsync(id, ratingParams);

            var response = ApiResponse<dynamic>.Success("Lấy dữ liệu thành công", result, StatusCodes.Status200OK);
            return Ok(response);
        }

        [HttpPost("rating")]
        public async Task<IActionResult> RatingPaidOrder([FromForm] RatingRequestDto request)
        {
            await _ratingService.RatingPaidOrderAsync(request);

            var response = ApiResponse<dynamic>.Success("Đánh giá thành công", null, StatusCodes.Status201Created);
            return CreatedAtAction(null, response);
        }
        #endregion


        #region advertisement endpoint
        [HttpGet("advertisements")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAdvertisement()
        {
            var result = await _advertisementService.GetAdvertisementsAsync();
            var response = ApiResponse<dynamic>.Success("Lấy dữ liệu thành công", result, StatusCodes.Status200OK);

            return Ok(response);
        }
        #endregion


        #region search endpoint
        [AllowAnonymous]
        [HttpGet("searching")]
        public async Task<IActionResult> SearchingMenu([FromQuery] SearchRequestDto searchRequest)
        {
            var result = await _menuService.SearchMenuAsync(searchRequest);

            var response = ApiResponse<dynamic>.Success("Lấy dữ liệu thành công", result, StatusCodes.Status200OK);
            return Ok(response);
        }
        #endregion
    }
}
