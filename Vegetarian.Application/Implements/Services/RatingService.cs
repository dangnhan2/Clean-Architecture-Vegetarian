using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Abstractions.Notifications;
using Vegetarian.Application.Abstractions.Storage;
using Vegetarian.Application.Dtos.QueryParams;
using Vegetarian.Application.Dtos.Request;
using Vegetarian.Application.Dtos.Response;
using Vegetarian.Application.Helper;
using Vegetarian.Application.Implements.Interface;
using Vegetarian.Application.Validator;
using Vegetarian.Domain.Enum;
using Vegetarian.Domain.Models;

namespace Vegetarian.Application.Implements.Services
{
    public class RatingService : IRatingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICloudinaryStorage _cloudinaryService;
        private const string folder = "RatingImage";
        private readonly INotificationSender _notificationSenderRepo;

        public RatingService(
            IUnitOfWork unitOfWork,
            ICloudinaryStorage cloudinaryService,
            INotificationSender notificationSenderRepo
        )
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
            _notificationSenderRepo = notificationSenderRepo;
        }

        public async Task<PagingResponse<RatingDto>> GetAllRatingsByMenuAsync(Guid menuId, RatingParams ratingParams)
        {
            var ratingsByMenu = _unitOfWork.Rating.GetAll().Where(r => r.MenuId == menuId);

            if (ratingParams.Stars.HasValue)
                ratingsByMenu = ratingsByMenu.Where(r => r.Stars == ratingParams.Stars.Value);

            var ratings = ratingsByMenu
                          .OrderByDescending(r => r.RatingAt)
                          .AsNoTracking()
                          .Select(r => new RatingDto
                          {
                              Id = r.Id,
                              MenuId = menuId,
                              CustomerUserName = r.User.UserName,
                              Comment = r.Comment,
                              Stars = r.Stars,
                              RatingAt = r.RatingAt.FormatDateTimeOffset(),
                              Images = r.Images.Select(i => i.ImageUrl).ToList(),
                              ResponseComment = r.ResponseRating != null ? r.ResponseRating.Comment : null,
                              AdminUserName = r.ResponseRating != null ? r.ResponseRating.User.UserName : null,
                              ResponseAt = r.ResponseRating != null ? r.ResponseRating.ResponseAt.FormatDateTimeOffset() : null
                          });

            if (ratingParams.Page != 0 && ratingParams.PageSize != 0)
                ratings = ratings.Paging(ratingParams.Page, ratingParams.PageSize);

            var response = new PagingResponse<RatingDto>(ratingParams.Page, ratingParams.PageSize, ratingsByMenu.Count(), await ratings.ToListAsync());
            return response;
        }

        public async Task RatingPaidOrderAsync(RatingRequestDto request)
        {
            var result = await new RatingValidator().ValidateAsync(request);

            if (!result.IsValid) throw new ValidationDictionaryException(result.ToDictionary());

            var hasPurchased = await _unitOfWork.OrderMenu
                .GetAll()
                .AnyAsync(o => o.OrderId == request.OrderId
                            && o.Orders.Status == OrderStatus.Confirmed
                            && o.MenuId == request.MenuId
                            && o.Orders.UserId == request.UserId);

            if (!hasPurchased) throw new InvalidDataException("Bạn chưa đặt món này");

            var hasRated = await _unitOfWork.Rating
                .GetAll()
                .AnyAsync(r => r.OrderId == request.OrderId
                            && r.MenuId == request.MenuId
                            && r.UserId == request.UserId);

            if (hasRated) throw new InvalidOperationException("Bạn đã đánh giá món ăn trong đơn hàng này rồi");

            var newRating = await MappingRating(request);

            var avg = await _unitOfWork.Rating.GetAverageRating(request.MenuId);

            if (avg == 0) avg = request.Stars;

            var menu = await _unitOfWork.Menu.GetByIdAsync(request.MenuId);

            if (menu != null) menu.AverageRating = avg;

            await _unitOfWork.Rating.AddAsync(newRating);
            await _unitOfWork.SaveChangeAsync();

            await _notificationSenderRepo.NotifyAdminWhenMenuRatedAsync(request.Comment, request.UserName, menu.Name, menu.Id);
        }

        public async Task ResponseRatingAsync(ResponseRatingRequestDto responseRatingRequest)
        {
            var result = await new ResponseRatingValidator().ValidateAsync(responseRatingRequest);

            if (!result.IsValid) throw new ValidationDictionaryException(result.ToDictionary());

            var existRating = await _unitOfWork.Rating.GetByIdAsync(responseRatingRequest.RatingId) ?? throw new KeyNotFoundException("Không tìm thấy đánh giá");

            var existUser = await _unitOfWork.User.GetByIdAsync(responseRatingRequest.UserId) ?? throw new KeyNotFoundException("Không tìm thấy người dùng");

            var responseRating = MappingResponseRating(responseRatingRequest);

            _unitOfWork.ResponseRating.Update(responseRating);
            await _unitOfWork.SaveChangeAsync();
        }


        #region
        private async Task<Rating> MappingRating(RatingRequestDto request)
        {
            var newRating = new Rating
            {
                Id = Guid.NewGuid(),
                MenuId = request.MenuId,
                UserId = request.UserId,
                OrderId = request.OrderId,
                Stars = request.Stars,
                Comment = request.Comment,
            };

            if (request.Images.Count > 0)
            {
                foreach (var image in request.Images)
                {
                    var imageUrl = await _cloudinaryService.UploadImage(image, folder);
                    var ratingImage = new RatingImage
                    {
                        Id = Guid.NewGuid(),
                        RatingId = newRating.Id,
                        ImageUrl = imageUrl,
                    };

                    newRating.Images.Add(ratingImage);
                }
            }

            return newRating;
        }

        private ResponseRating MappingResponseRating(ResponseRatingRequestDto responseRatingRequest)
        {
            var responseRating = new ResponseRating
            {
                UserId = responseRatingRequest.UserId,
                RatingId = responseRatingRequest.RatingId,
                Comment = responseRatingRequest.Comment,
            };

            return responseRating;
        }
        #endregion
    }
}
