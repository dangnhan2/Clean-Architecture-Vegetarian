using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Dtos.Request;
using Vegetarian.Application.Dtos.Response;

namespace Vegetarian.Application.Implements.Interface
{
    public interface IAdvertisementService
    {
        public Task<IEnumerable<AdvertisementDto>> GetAdvertisementsByAdminAsync();
        public Task<IEnumerable<AdvertisementDto>> GetAdvertisementsAsync();
        public Task AddAdvertisementAsync(AdvertisementRequestDto advertisementRequest);
        public Task UpdateAdvertisementAsync(Guid adId, AdvertisementRequestDto advertisementRequest);     
    }
}
