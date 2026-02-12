using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Dtos.QueryParams;
using Vegetarian.Application.Dtos.Request;
using Vegetarian.Application.Dtos.Response;

namespace Vegetarian.Application.Implements.Interface
{
    public interface IUserService
    {
        public Task<PagingResponse<UserDto>> GetAllAsync(UserParams userParams);
        public Task UploadProfileAsync(Guid userId, UserRequestDto request);
        public Task<UserDto> GetUserByIdAsync(Guid userId);
        public Task BanUserAsync(Guid userId);
        public Task UnBanUserAsync(Guid userId);
    }
}
