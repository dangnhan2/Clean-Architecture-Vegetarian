using RedLockNet;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application;
using Vegetarian.Application.Abstractions.BackgroundJobs;
using Vegetarian.Application.Abstractions.Caching;
using Vegetarian.Application.Contants;
using Vegetarian.Domain.Enum;

namespace Vegetarian.Infrastructure.Services.BackgroundJobs
{
    public class Jobs : IJobs
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDistributedLockFactory _redLockFactory;
        private readonly ICachingProvider _cachingProvider;

        public Jobs(IUnitOfWork unitOfWork, IDistributedLockFactory redLockFactory, ICachingProvider cachingProvider)
        {
            _unitOfWork = unitOfWork;
            _redLockFactory = redLockFactory;
            _cachingProvider = cachingProvider;
        }

        public async Task RecurringDeleteExpiredCartsJob_3hours()
        {
            var carts = _unitOfWork.Cart
                .GetAll()
                .Where(c => c.CreatedAt.AddHours(3) < DateTime.UtcNow);

            if (carts.Count() > 0)
            {
                _unitOfWork.Cart.RemoveRange(carts);
                await _unitOfWork.SaveChangeAsync();
            }
        }

        public async Task RecurringDeleteExpiredOtpJob_5mins()
        {
            var expiredOtps = _unitOfWork.EmailOtp
                .GetAll()
                .Where(otp => otp.ExpiredAt < DateTime.UtcNow);

            _unitOfWork.EmailOtp.RemoveRange(expiredOtps);
            await _unitOfWork.SaveChangeAsync();
        }

        public async Task RecurringDeleteExpiredRefreshTokensJob_3months()
        {
            var tokens = _unitOfWork.RefreshToken
                .GetAll()
                .Where(t => t.ExpriedAt < DateTime.UtcNow);


            if (tokens.Count() > 0)
            {
                _unitOfWork.RefreshToken.RemoveRange(tokens);
                await _unitOfWork.SaveChangeAsync();
            }
        }

        public async Task RecurringDeleteNotificationsJob_1month()
        {
            DateTime todayUtc = DateTime.UtcNow.Date;
            DateTime tomorrowUtc = todayUtc.AddDays(1);

            var notifications = _unitOfWork.Notification.GetAll().Where(x => x.CreatedAt.Date.AddDays(30) >= todayUtc && x.CreatedAt.Date.AddDays(30) < tomorrowUtc);

            if (notifications.Count() > 0)
            {
                _unitOfWork.Notification.RemoveRange(notifications);
                await _unitOfWork.SaveChangeAsync();

            }
        }

        public async Task RecurringPublicVouchersJob_1hour()
        {
            var vouchers = _unitOfWork.Voucher
                .GetAll()
                .Where(v => v.StartDate <= DateTimeOffset.UtcNow && !v.IsActive);

            Log.Information($"Voucher cần active: {vouchers.Count()}");

            if (vouchers.Count() > 0)
            {
                foreach (var voucher in vouchers)
                    voucher.IsActive = true;

                await _unitOfWork.SaveChangeAsync();
                Log.Information("Đã publish voucher thành công.");
            }
            else
            {
                Log.Information("Không có voucher nào cần publish hôm nay.");
            }

            Log.Information("Finish publishing voucher");
        }

        public async Task RecurringResetVoucherRedemptionsJob_24hours()
        {
            var voucherRedemptions = _unitOfWork.VoucherRedemption.GetAll();

            if (voucherRedemptions.Count() > 0)
            {
                _unitOfWork.VoucherRedemption.RemoveRange(voucherRedemptions);
                await _unitOfWork.SaveChangeAsync();
            }
        }

        public async Task RecurringRetrieveVouchersJob_1hour()
        {
            var vouchers = _unitOfWork.Voucher
                .GetAll()
                .Where(v => v.EndDate <= DateTimeOffset.UtcNow && v.IsActive);

            Log.Information($"Voucher cần thu hồi: {vouchers.Count()}");

            if (vouchers.Count() > 0)
            {
                foreach (var voucher in vouchers)
                    voucher.IsActive = false;

                await _unitOfWork.SaveChangeAsync();
                Log.Information("✅ Đã thu hồi voucher hết hạn.");
            }
            else
            {
                Log.Information("Không có voucher nào hết hạn hôm nay.");
            }

            Log.Information("Finish retrieving voucher");
        }

        public async Task SchedulePublicVoucher(Guid voucherId)
        {
            var voucher = await _unitOfWork.Voucher.GetByIdAsync(voucherId) ?? throw new KeyNotFoundException("Không tìm thấy mã giảm giá");

            if (voucher.EndDate < DateTimeOffset.UtcNow) throw new InvalidDataException("Mã giảm giá đã hết hạn");

            voucher.IsActive = true;

            _unitOfWork.Voucher.Update(voucher);
            await _unitOfWork.SaveChangeAsync();

            await _cachingProvider.RemoveAsync(CacheKeys.VOUCHER_ACTIVE);
        }

        public async Task ScheduleRetrieveVoucher(Guid voucherId)
        {
            var voucher = await _unitOfWork.Voucher.GetByIdAsync(voucherId) ?? throw new KeyNotFoundException("Không tìm thấy mã giảm giá");

            if (!voucher.IsActive) return;

            voucher.IsActive = false;

            _unitOfWork.Voucher.Update(voucher);
            await _unitOfWork.SaveChangeAsync();

            await _cachingProvider.RemoveAsync(CacheKeys.VOUCHER_ACTIVE);
        }

        public async Task ScheduleUpdateOrderExpiredJob_10mins(Guid orderId)
        {
            var order = await _unitOfWork.Order.GetByIdAsync(orderId) ?? throw new KeyNotFoundException("Không tìm thấy đơn hàng");

            if (order.Status != OrderStatus.Pending)
                return;

            var voucherRedemption = await _unitOfWork.VoucherRedemption.GetVoucherRedemptionsByOrderIdAsync(order.Id);

            if (voucherRedemption != null)
            {
                await using (var redLock = await _redLockFactory.CreateLockAsync($"lock:voucher:{voucherRedemption.VoucherID}", TimeSpan.FromSeconds(30)))
                {
                    await _unitOfWork.BeginTransactionAsync();

                    try
                    {
                        voucherRedemption.Voucher.ReservedCount--;

                        order.Status = OrderStatus.Cancelled;

                        _unitOfWork.VoucherRedemption.Remove(voucherRedemption);
                        _unitOfWork.Order.Update(order);
                        await _unitOfWork.SaveChangeAsync();
                        await _unitOfWork.CommitTransactionAsync();
                    }
                    catch
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        throw;
                    }
                }

            }

            order.Status = OrderStatus.Cancelled;
            _unitOfWork.Order.Update(order);
            await _unitOfWork.SaveChangeAsync();
        }
    }
}
