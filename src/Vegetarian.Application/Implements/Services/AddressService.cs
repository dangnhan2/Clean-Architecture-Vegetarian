using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Abstractions.Caching;
using Vegetarian.Application.Contants;
using Vegetarian.Application.Dtos.Request;
using Vegetarian.Application.Dtos.Response;
using Vegetarian.Application.Implements.Interface;
using Vegetarian.Application.Validator;
using Vegetarian.Domain.Models;

namespace Vegetarian.Application.Implements.Services
{
    public class AddressService : IAddressService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICachingProvider _cacheService;

        public AddressService(IUnitOfWork unitOfWork, ICachingProvider cacheService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
        }

        public async Task AddAsync(AddressRequestDto request)
        {
            var result = await new AddressValidator().ValidateAsync(request);

            if (!result.IsValid)
                throw new ValidationDictionaryException(result.ToDictionary());

            var newAddress = await MappingAddress(request);

            await _unitOfWork.Address.AddAsync(newAddress);
            await _unitOfWork.SaveChangeAsync();

            await _cacheService.RemoveAsync(CacheKeys.UserAddresses(newAddress.UserId));
        }

        public async Task DeleteAsync(Guid addressId)
        {
            var address = await _unitOfWork.Address.GetByIdAsync(addressId);

            if (address == null)
                throw new KeyNotFoundException("Không tìm thấy địa chỉ");

            if (address.IsDefault)
                throw new InvalidDataException("Địa chỉ của bạn đang là mặc định, hãy chọn địa chỉ khác làm địa chỉ mặc định");

            _unitOfWork.Address.Remove(address);
            await _unitOfWork.SaveChangeAsync();
            await _cacheService.RemoveAsync(CacheKeys.UserAddresses(address.UserId));
        }

        public async Task<IEnumerable<AddressDto>> GetAllByUserAsync(Guid userId)
        {
            string cacheKey = CacheKeys.UserAddresses(userId);
            var cacheAddresses = await _cacheService.GetAsync<IEnumerable<AddressDto>>(cacheKey);
            if (cacheAddresses != null)
                return cacheAddresses;

            var addresses = _unitOfWork.Address.GetAll().Where(a => a.UserId == userId);

            var addressesToDto = await addresses
                .OrderByDescending(a => a.IsDefault)
                .AsNoTracking()
                .Select(a => new AddressDto
                {
                    Id = a.Id,
                    Address = a.AddressName,
                    FullName = a.FullName,
                    PhoneNumber = a.PhoneNumber,
                    Province = a.Province,
                    District = a.District,
                    IsDefault = a.IsDefault,
                })
                .ToListAsync();

            await _cacheService.SetAsync(cacheKey, addressesToDto, TimeSpan.FromHours(1));

            return addressesToDto;
        }

        public async Task SetAddressAsDefault(Guid addressId)
        {
            var address = await _unitOfWork.Address.GetByIdAsync(addressId) ?? throw new KeyNotFoundException("Không tìm thấy địa chỉ");

            var addresses = _unitOfWork.Address
                .GetAll()
                .Where(a => a.UserId == address.UserId);

            await addresses.ExecuteUpdateAsync(add => add.SetProperty(x => x.IsDefault, false));

            address.IsDefault = true;

            _unitOfWork.Address.Update(address);
            await _unitOfWork.SaveChangeAsync();
            await _cacheService.RemoveAsync(CacheKeys.UserAddresses(address.UserId));
        }

        public async Task UpdateAsync(Guid addressId, AddressRequestDto request)
        {
            var result = await new AddressValidator().ValidateAsync(request);

            if (!result.IsValid)
                throw new ValidationDictionaryException(result.ToDictionary());

            var address = await _unitOfWork.Address
                .GetByIdAsync(addressId);

            if (address == null)
                throw new KeyNotFoundException("Không tìm thấy địa chỉ");

            var isDefault = address.IsDefault;

            address.FullName = request.FullName;
            address.PhoneNumber = request.PhoneNumber;
            address.AddressName = request.Address;
            address.UserId = request.UserId;
            address.Province = request.Province;
            address.District = request.District;

            if (isDefault != request.IsDefault)
            {
                var addresses = _unitOfWork.Address
                    .GetAll()
                    .Where(a => a.UserId == address.UserId);

                await addresses.ExecuteUpdateAsync(a => a.SetProperty(x => x.IsDefault, false));
                address.IsDefault = request.IsDefault;
            }

            _unitOfWork.Address.Update(address);
            await _unitOfWork.SaveChangeAsync();

            await _cacheService.RemoveAsync(CacheKeys.UserAddresses(address.UserId));
        }


        #region helper method
        private async Task<Address> MappingAddress(AddressRequestDto request)
        {
            Address address = new Address
            {
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                AddressName = request.Address,
                Province = request.Province,
                District = request.District,
                UserId = request.UserId,
            };

            if (request.IsDefault)
            {
                var addresses = _unitOfWork.Address
                    .GetAll()
                    .Where(a => a.UserId == request.UserId);

                await addresses.ExecuteUpdateAsync(a => a.SetProperty(x => x.IsDefault, false));
                address.IsDefault = request.IsDefault;
            }

            return address;
        }
        #endregion
    }
}
