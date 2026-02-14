
using System.Data;
using System.Net;
using System.Text.Json;
using Vegetarian.Application.Dtos.Response;
using Vegetarian.Application.Validator;

namespace Vegetarian.API.Middleware
{
    public class GlobalException
    {
        private readonly RequestDelegate _next;

        public GlobalException(RequestDelegate next) {
           _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ArgumentException ex) // lỗi đầu vào
            {
                LogHelper.LogError(ex);
                await HandleExceptionAsync(context, ex, HttpStatusCode.BadRequest);
            }
            catch (KeyNotFoundException ex) // lỗi không tìm thấy dữ liệu hoặc null
            {
                LogHelper.LogError(ex);
                await HandleExceptionAsync(context, ex, HttpStatusCode.NotFound);
            }
            catch (UnauthorizedAccessException ex) // lỗi xác thực/ủy quyền
            {
                LogHelper.LogError(ex);
                await HandleExceptionAsync(context, ex, HttpStatusCode.Unauthorized);
            }
            catch (DuplicateNameException ex) // lỗi cùng giá trị 
            {
                LogHelper.LogError(ex);
                await HandleExceptionAsync(context, ex, HttpStatusCode.BadRequest);
            }
            catch (ValidationDictionaryException ex) // lỗi validation
            {
                LogHelper.LogError(ex);
                await HandleExceptionAsync(context, ex, HttpStatusCode.BadRequest);
            }
            catch (InvalidDataException ex) // lỗi dữ liệu ở db
            {
                LogHelper.LogError(ex);
                await HandleExceptionAsync(context, ex, HttpStatusCode.BadRequest);
            }
            catch (InvalidOperationException ex)
            {
                LogHelper.LogError(ex);
                await HandleExceptionAsync(context, ex, HttpStatusCode.BadRequest);
            }
            catch (Exception ex) // các lỗi còn lại
            {
                LogHelper.LogError(ex);
                await HandleExceptionAsync(context, ex, HttpStatusCode.InternalServerError);
            }
        }

        public async Task HandleExceptionAsync(HttpContext context, Exception ex, HttpStatusCode statusCode)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            string message;
            object? data = null;

            switch (ex)
            {
                case InvalidDataException invalidDataException:
                    message = invalidDataException.Message;
                    break;
                case InvalidOperationException invalidOperationException:
                    message = invalidOperationException.Message;
                    break;
                case ValidationDictionaryException validationEx:
                    message = string.Join(Environment.NewLine, validationEx.Errors.Select(e => $"{e.Key}=[{string.Join(", ", e.Value)}]"));
                    break;
                case ArgumentException argEx:
                    message = argEx.Message;
                    break;
                case KeyNotFoundException keyEx:
                    message = keyEx.Message;
                    break;
                case UnauthorizedAccessException:
                    message = ex.Message;
                    break;
                case DuplicateNameException dupEx:
                    message = dupEx.Message;
                    break;
                default:
                    message = "Hệ thống đang gặp sự cố. Vui lòng thử lại sau.";
                    break;
            }

            var response = new ApiResponse<object>(message, false, (int)statusCode, data);

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));

        }
    }
}
