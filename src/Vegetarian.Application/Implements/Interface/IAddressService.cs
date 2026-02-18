using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Dtos.Request;
using Vegetarian.Application.Dtos.Response;

namespace Vegetarian.Application.Implements.Interface
{
    public interface IAddressService
    {
        public Task SetAddressAsDefault(Guid addressId);
        public Task<IEnumerable<AddressDto>> GetAllByUserAsync(Guid userId);
        public Task AddAsync(AddressRequestDto request);
        public Task UpdateAsync(Guid addressId, AddressRequestDto request);
        public Task DeleteAsync(Guid addressId);
    }
}
