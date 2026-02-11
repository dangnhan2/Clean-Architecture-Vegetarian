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
    public interface IVoucherService
    {
        public Task<PagingResponse<VoucherDto>> GetAllByAdminAsync(VoucherParams voucherParams);
        public Task<IEnumerable<VoucherDto>> GetAllByCustomerAsync();
        public Task<VoucherDto> GetByIdAsync(Guid voucherId);
        public Task AddAsync(VoucherRequestDto request);
        public Task UpdateAsync(Guid voucherId, VoucherRequestDto request);
        public Task DeleteAsync(Guid voucherId);
        public Task<VoucherValidationDto> ValidateVoucherAsync(ValidationVoucherRequestDto request);
    }
}
