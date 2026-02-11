using Vegetarian.Application.Repositories;

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
        Task SaveChangeAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
