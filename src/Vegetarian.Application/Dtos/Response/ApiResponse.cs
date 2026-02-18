using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Vegetarian.Application.Dtos.Response
{
    public class ApiResponse<T>
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonPropertyName("statusCode")]
        public int StatusCode { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }

        public ApiResponse(string message, bool success, int code, T data)
        {
            Message = message;
            IsSuccess = success;
            StatusCode = code;
            Data = data;
        }

        public static ApiResponse<T> Success(string? message, T data, int code)
        {
            return new ApiResponse<T>(message, true, code, data);
        }

        public static ApiResponse<T> Fail(dynamic message, int code)
        {
            return new ApiResponse<T>(message, false, code, default(T));
        }
    }
}
