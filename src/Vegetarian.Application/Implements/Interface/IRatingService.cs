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
    public interface IRatingService
    {
        public Task<PagingResponse<RatingDto>> GetAllRatingsByMenuAsync(Guid menuId, RatingParams ratingParams);
        public Task RatingPaidOrderAsync(RatingRequestDto request);
        public Task ResponseRatingAsync(ResponseRatingRequestDto responseRatingRequest);
    }
}
