using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Vegetarian.Application.Abstractions.Payment;
using Vegetarian.Application.Dtos.Request;
using Vegetarian.Application.Dtos.Response;

namespace Vegetarian.API.Controllers
{
    [Route("api/payment")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentGateway _payOsService;

        public PaymentController(IPaymentGateway payOsService)
        {
            _payOsService = payOsService;
        }

        [HttpPost("webhook/confirm")]
        public async Task<IActionResult> ConfirmWebHook([FromBody] WebHookUrlRequestDto request)
        {
            var result = await _payOsService.ConfirmWebHook(request);

            return Ok(result);
        }

        [HttpPost("callback")]
        public async Task<IActionResult> CallBack()
        {

            //var result = await _payOsService.CallBack(Request);

            //var response = ApiResponse<dynamic>.Success("Thanh toán thành công", result, StatusCodes.Status200OK);

            //return Ok(response);

            return new JsonResult(new { }) { StatusCode = 200 };
        }
    }
}
