using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Abstractions.Caching;
using Vegetarian.Application.Contants;
using Vegetarian.Application.Dtos.QueryParams;
using Vegetarian.Application.Dtos.Request;
using Vegetarian.Application.Dtos.Response;
using Vegetarian.Application.Helper;
using Vegetarian.Application.Implements.Interface;
using Vegetarian.Application.Validator;
using Vegetarian.Domain.Models;

namespace Vegetarian.Application.Implements.Services
{
    public class VoucherService : IVoucherService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICachingProvider _cacheService;
        private int TAX_RATE = 8;
        public VoucherService(IUnitOfWork unitOfWork, ICachingProvider cacheService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
        }

        public async Task AddAsync(VoucherRequestDto request)
        {
            var result = await new VoucherValidator().ValidateAsync(request);

            if (!result.IsValid)
                throw new ValidationDictionaryException(result.ToDictionary());

            if (request.IsActive && request.StartDate > DateTimeOffset.UtcNow)
                throw new ArgumentException("Thời điểm bắt đầu voucher đang khác với giờ hiện tại, hãy sửa lại giờ bắt đầu phù hợp");

            var newVoucher = MappingVoucher(request);

            await _unitOfWork.Voucher.AddAsync(newVoucher);
            await _unitOfWork.SaveChangeAsync();

            if (newVoucher.IsActive)
                await _cacheService.RemoveAsync(CacheKeys.VOUCHER_ACTIVE);
        }

        public async Task DeleteAsync(Guid voucherId)
        {
            var existVoucher = await _unitOfWork.Voucher.GetByIdAsync(voucherId);

            if (existVoucher == null)
                throw new KeyNotFoundException("Mã giảm giá không tồn tại");

            if (existVoucher.IsActive)
                throw new InvalidOperationException("Mã giảm giá đang được áp dụng. Hãy cập nhật lại trước khi xóa");

            _unitOfWork.Voucher.Remove(existVoucher);
            await _unitOfWork.SaveChangeAsync();
            await _cacheService.RemoveAsync(CacheKeys.VoucherDetail(existVoucher.Id));
        }

        public async Task<PagingResponse<VoucherDto>> GetAllByAdminAsync(VoucherParams voucherParams)
        {
            var vouchers = _unitOfWork.Voucher.GetAll();

            if (!string.IsNullOrEmpty(voucherParams.Search))
                vouchers = vouchers.Where(v => EF.Functions.ILike(EF.Functions.Unaccent(v.Code), "%" + EF.Functions.Unaccent(voucherParams.Search) + "%"));

            if (voucherParams.From.HasValue && voucherParams.To.HasValue)
                vouchers = vouchers.Where(v => v.StartDate == voucherParams.From.Value && v.EndDate == voucherParams.To.Value);

            var voucherToDTO = vouchers
                    .Select(v => new VoucherDto
                    {
                        Id = v.Id,
                        Code = v.Code,
                        Description = v.Description,
                        DiscountType = v.DiscountType,
                        DiscountValue = v.DiscountValue,
                        StartDate = v.StartDate.FormatDateTimeOffset(),
                        EndDate = v.EndDate.FormatDateTimeOffset(),
                        MaxDiscount = v.MaxDiscount,
                        MinOrderAmount = v.MinOrderAmount,
                        PerUserLimit = v.PerUserLimit,
                        UsageLimit = v.UsageLimit,
                        UsedCount = v.UsedCount,
                        IsActive = v.IsActive
                    })
                    .AsNoTracking();

            if (voucherParams.Page != 0 && voucherParams.PageSize != 0)
                voucherToDTO = voucherToDTO.Paging(voucherParams.Page, voucherParams.PageSize);

            var response = new PagingResponse<VoucherDto>(voucherParams.Page, voucherParams.PageSize, vouchers.Count(), await voucherToDTO.ToListAsync());
            return response;
        }

        public async Task<IEnumerable<VoucherDto>> GetAllByCustomerAsync()
        {
            string cacheKey = CacheKeys.VOUCHER_ACTIVE;
            var cacheVouchers = await _cacheService.GetAsync<IEnumerable<VoucherDto>>(cacheKey);
            if (cacheVouchers != null)
                return cacheVouchers;

            var vouchers = _unitOfWork.Voucher.GetAll();

            var vouchersToDTO = await vouchers
                .Where(v => v.IsActive)
                .Select(v => new VoucherDto
                {
                    Id = v.Id,
                    Code = v.Code,
                    Description = v.Description,
                    DiscountType = v.DiscountType,
                    DiscountValue = v.DiscountValue,
                    StartDate = v.StartDate.FormatDateTimeOffset(),
                    EndDate = v.EndDate.FormatDateTimeOffset(),
                    MaxDiscount = v.MaxDiscount,
                    MinOrderAmount = v.MinOrderAmount,
                    PerUserLimit = v.PerUserLimit,
                    UsageLimit = v.UsageLimit,
                    UsedCount = v.UsedCount,
                    IsActive = v.IsActive
                })
                .AsNoTracking()
                .ToListAsync();

            await _cacheService.SetAsync(cacheKey, vouchersToDTO, TimeSpan.FromMinutes(30));

            return vouchersToDTO;
        }

        public async Task<VoucherDto> GetByIdAsync(Guid voucherId)
        {
            var cacheKey = CacheKeys.VoucherDetail(voucherId);
            var cached = await _cacheService.GetAsync<VoucherDto>(cacheKey);
            if (cached != null) return cached;

            var voucher = await _unitOfWork.Voucher.GetByIdAsync(voucherId) ?? throw new KeyNotFoundException("Mã giảm giá không tồn tại");

            var voucherToDto = new VoucherDto
            {
                Id = voucher.Id,
                Code = voucher.Code,
                Description = voucher.Description,
                DiscountType = voucher.DiscountType,
                DiscountValue = voucher.DiscountValue,
                StartDate = voucher.StartDate.FormatDateTimeOffset(),
                EndDate = voucher.EndDate.FormatDateTimeOffset(),
                MaxDiscount = voucher.MaxDiscount,
                MinOrderAmount = voucher.MinOrderAmount,
                PerUserLimit = voucher.PerUserLimit,
                UsageLimit = voucher.UsageLimit,
                UsedCount = voucher.UsedCount,
                IsActive = voucher.IsActive
            };

            await _cacheService.SetAsync(cacheKey, voucherToDto, TimeSpan.FromHours(12));
            return voucherToDto;
        }

        public async Task UpdateAsync(Guid voucherId, VoucherRequestDto request)
        {
            var result = await new VoucherValidator().ValidateAsync(request);

            if (!result.IsValid)
                throw new ValidationDictionaryException(result.ToDictionary());

            if (request.IsActive && request.StartDate > DateTimeOffset.UtcNow)
                throw new ArgumentException("Thời điểm bắt đầu voucher đang khác với giờ hiện tại, hãy sửa lại giờ bắt đầu phù hợp");

            var existVoucher = await _unitOfWork.Voucher.GetByIdAsync(voucherId) ?? throw new KeyNotFoundException("Mã giảm giá không tồn tại");

            var oldIsActive = existVoucher.IsActive;

            existVoucher.Code = request.Code.ToUpper();
            existVoucher.Description = $"Hạn sử dụng {request.StartDate.FormatDateTimeOffset()} đến ngày {request.EndDate.FormatDateTimeOffset()}";
            existVoucher.DiscountType = request.DiscountType;
            existVoucher.DiscountValue = request.DiscountValue;
            existVoucher.StartDate = request.StartDate;
            existVoucher.EndDate = request.EndDate;
            existVoucher.MaxDiscount = request.MaxDiscount;
            existVoucher.MinOrderAmount = request.MinOrderAmount;
            existVoucher.PerUserLimit = request.PerUserLimit;
            existVoucher.UsedCount = 0;
            existVoucher.UsageLimit = request.UsageLimit;
            existVoucher.IsActive = request.IsActive;

            if (oldIsActive != request.IsActive)
                await _cacheService.RemoveAsync(CacheKeys.VOUCHER_ACTIVE);

            _unitOfWork.Voucher.Update(existVoucher);
            await _cacheService.RemoveAsync(CacheKeys.VoucherDetail(existVoucher.Id));
            await _unitOfWork.SaveChangeAsync();
        }

        public async Task<VoucherValidationDto> ValidateVoucherAsync(ValidationVoucherRequestDto request)
        {
            var voucher = await _unitOfWork.Voucher.GetByIdAsync(
                v => v.Id == request.VoucherId
                && v.StartDate <= DateTime.UtcNow
                && v.EndDate >= DateTime.UtcNow
                && v.UsedCount < v.UsageLimit
                && v.IsActive) ?? throw new KeyNotFoundException("Mã giảm giá không tồn tại");


            var cart = await _unitOfWork.Cart.GetCartByCustomerAsync(request.UserId);

            if (cart == null || cart.CartItems.Count == 0)
                throw new KeyNotFoundException("Giỏ hàng trống / không tồn tại");

            decimal subTotal = GetTotalAmount(cart.CartItems);

            // check if user already used this voucher in the same day
            var todayCount = await _unitOfWork.VoucherRedemption.TodayCountAsync(request.UserId, voucher.Id);

            if (todayCount >= voucher.PerUserLimit)
                throw new InvalidDataException("Bạn đã sử dụng voucher này hôm nay rồi");

            if (voucher.MinOrderAmount > subTotal)
                throw new InvalidDataException($"Đơn hàng phải đạt giá trị tối thiểu {voucher.MinOrderAmount}");

            // calculate tax
            subTotal = subTotal + subTotal * TAX_RATE / 100;

            decimal discountAmount = voucher.DiscountType == "percent"
                ? subTotal * voucher.DiscountValue / 100
                : voucher.DiscountValue;

            discountAmount = Math.Min(discountAmount, voucher.MaxDiscount);

            decimal totalAmount = subTotal - discountAmount;

            return new VoucherValidationDto
            {
                DiscountAmount = discountAmount,
                TotalAmount = totalAmount
            };
        }


        #region helper method
        private decimal GetTotalAmount(ICollection<CartItem> items)
        {
            decimal subTotal = 0;
            foreach (var item in items)
            {
                subTotal += item.Quantity * item.UnitPrice;
            }
            return subTotal;
        }

        private Voucher MappingVoucher(VoucherRequestDto request)
        {
            var voucher = new Voucher
            {
                Code = request.Code.ToUpper(),
                Description = $"Hạn sử dụng {request.StartDate.FormatDateTimeOffset()} đến ngày {request.EndDate.FormatDateTimeOffset()}",
                DiscountType = request.DiscountType,
                DiscountValue = request.DiscountValue,
                StartDate = request.StartDate,
                EndDate = request.StartDate,
                MaxDiscount = request.MaxDiscount,
                MinOrderAmount = request.MinOrderAmount,
                PerUserLimit = request.PerUserLimit,
                UsageLimit = request.UsageLimit,
                UsedCount = 0,
                IsActive = request.IsActive,
            };

            if (request.DiscountType != "percent")
            {
                voucher.MaxDiscount = request.DiscountValue;
            }

            return voucher;
        }
        #endregion
    }
}
