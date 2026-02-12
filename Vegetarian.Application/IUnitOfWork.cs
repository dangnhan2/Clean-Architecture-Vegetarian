using Vegetarian.Application.Abstractions.Persistence;

namespace Vegetarian.Application
{
    public interface IUnitOfWork
    {
        IUserRepo User { get; }
        IRefreshTokenRepo RefreshToken { get; }
        IEmailOtpRepo EmailOtp { get; }
        ICategoryRepo Category { get; }
        IMenuRepo Menu { get; }
        ICartRepo Cart { get; }
        IVoucherRepo Voucher { get; }
        IVoucherRedemptionRepo VoucherRedemption { get; }
        IOrderRepo Order { get; }
        INotificationRepo Notification { get; }
        IAddressRepo Address { get; }
        Task SaveChangeAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
