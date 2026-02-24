using DotNetEnv;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Abstractions.Caching;
using Vegetarian.Application.Abstractions.Storage;
using Vegetarian.Application.Contants;
using Vegetarian.Application.Dtos.QueryParams;
using Vegetarian.Application.Dtos.Request;
using Vegetarian.Application.Dtos.Response;
using Vegetarian.Application.Helper;
using Vegetarian.Application.Implements.Interface;
using Vegetarian.Application.Validator;
using Vegetarian.Domain.Models;
using Vegetarian.Domain.Enum;

namespace Vegetarian.Application.Implements.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICloudinaryStorage _cloudinaryService;
        private readonly string _defaultAvatar;
        private const string folder = "Avatar";
        private readonly ICachingProvider _cachingService;
        private readonly UserManager<User> _userManager;

        public UserService(
            IUnitOfWork unitOfWork,
            ICloudinaryStorage cloudinaryService,
            ICachingProvider cachingService,
            UserManager<User> userManager)
        {
            Env.Load();
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
            _defaultAvatar = Env.GetString("DEFAULT_AVATAR");
            _cachingService = cachingService;
            _userManager = userManager;
        }


        public async Task BanUserAsync(Guid userId)
        {
            var user = await _unitOfWork.User.GetByIdAsync(userId);

            if (user == null)
                throw new KeyNotFoundException("Người dùng không tồn tại");

            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(10));

            var refreshTokens = _unitOfWork.RefreshToken
                .GetAll()
                .Where(rt => rt.UserId == userId);

            foreach (var rt in refreshTokens)
            {
                rt.IsRevoked = true;
            }

            await _unitOfWork.SaveChangeAsync();
        }

        public async Task<PagingResponse<UserDto>> GetAllAsync(UserParams userParams)
        {
            var users = _unitOfWork.User.GetAll().Where(u => !u.IsAdmin);

            if (!string.IsNullOrEmpty(userParams.Search))
            {
                users = users.Where(u =>
                EF.Functions.ILike(EF.Functions.Unaccent(u.UserName), "%" + EF.Functions.Unaccent(userParams.Search) + "%")
                || EF.Functions.ILike(EF.Functions.Unaccent(u.PhoneNumber), "%" + EF.Functions.Unaccent(userParams.Search) + "%")
                || EF.Functions.ILike(EF.Functions.Unaccent(u.Email), "%" + EF.Functions.Unaccent(userParams.Search) + "%"));
            }

            var usersToDTO = users
                    .OrderByDescending(u => u.CreatedAt)
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        UserName = u.UserName,
                        PhoneNumber = u.PhoneNumber,
                        ImageUrl = u.ImageUrl,
                        Email = u.Email,
                        IsActive = u.LockoutEnd.HasValue ? false : true,
                        TotalAmountInMonth = u.Orders
                        .Where(o => o.User.Id == u.Id && o.CreatedAt.Month == DateTimeOffset.UtcNow.Month && o.Status == OrderStatus.Confirmed)
                        .Sum(o => o.TotalAmount),
                        TotalAmountInYear = u.Orders
                        .Where(o => o.User.Id == u.Id && o.CreatedAt.Year == DateTimeOffset.UtcNow.Year && o.Status == OrderStatus.Confirmed)
                        .Sum(o => o.TotalAmount)

                    })
                    .AsNoTracking();


            if (userParams.Page != 0 && userParams.PageSize != 0)
                usersToDTO = usersToDTO.Paging(userParams.Page, userParams.PageSize);

            var response = new PagingResponse<UserDto>(userParams.Page, userParams.PageSize, users.Count(), await usersToDTO.ToListAsync());
            return response;
        }

        public async Task<UserDto> GetUserByIdAsync(Guid userId)
        {
            var cacheKey = CacheKeys.UserDetail(userId);
            var cached = await _cachingService.GetAsync<UserDto>(cacheKey);
            if (cached != null)
                return cached;

            var user = await _unitOfWork.User.GetByIdAsync(userId);

            if (user == null)
                throw new KeyNotFoundException("Người dùng không tồn tại");

            var response = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                PhoneNumber = user.PhoneNumber,
                ImageUrl = user.ImageUrl,
                Email = user.Email,
                IsActive = user.LockoutEnd.HasValue ? false : true,
                TotalAmountInMonth = user.Orders
                        .Where(o => o.User.Id == user.Id && o.CreatedAt.Month == DateTimeOffset.UtcNow.Month && o.Status == OrderStatus.Confirmed)
                        .Sum(o => o.TotalAmount),
                TotalAmountInYear = user.Orders
                        .Where(o => o.User.Id == user.Id && o.CreatedAt.Year == DateTimeOffset.UtcNow.Year && o.Status == OrderStatus.Confirmed)
                        .Sum(o => o.TotalAmount)
            };

            await _cachingService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(10));
            return response;
        }

        public async Task UnBanUserAsync(Guid userId)
        {
            var user = await _unitOfWork.User.GetByIdAsync(userId);

            if (user == null)
                throw new KeyNotFoundException("Người dùng không tồn tại");

            await _userManager.SetLockoutEndDateAsync(user, null);
        }

        public async Task UploadProfileAsync(Guid userId, UserRequestDto request)
        {
            var result = await new UserValidator().ValidateAsync(request);

            if (!result.IsValid)
                throw new ValidationDictionaryException(result.ToDictionary());

            var user = await _unitOfWork.User.GetByIdAsync(userId);

            if (user == null)
                throw new KeyNotFoundException("Người dùng không tồn tại");

            var phoneNumbers = _unitOfWork.User
                .GetAll()
                .Where(u => u.PhoneNumber == request.PhoneNumber && u.Id != userId);

            if (phoneNumbers.Any())
                throw new ArgumentException("Số điện thoại đã đăng kí");

            if (request.Avatar != null)
            {
                var url = await _cloudinaryService.UploadImage(request.Avatar, folder);

                if (user.ImageUrl != _defaultAvatar)
                {
                    await _cloudinaryService.DeleteImage(user.ImageUrl);
                    user.ImageUrl = url;
                }
                else
                {
                    user.ImageUrl = url;
                }
            }

            user.PhoneNumber = request.PhoneNumber;

            _unitOfWork.User.Update(user);
            await _unitOfWork.SaveChangeAsync();
            await _cachingService.RemoveAsync(CacheKeys.UserDetail(user.Id));
        }
    }
}
