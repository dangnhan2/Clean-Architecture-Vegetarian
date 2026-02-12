using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application;
using Vegetarian.Application.Abstractions.Persistence;
using Vegetarian.Domain.Models;
using Vegetarian.Infrastructure.Data;
using Vegetarian.Infrastructure.Repositories;

namespace Vegetarian.Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly VegetarianDbContext _context;
        public UnitOfWork(VegetarianDbContext context)
        {
            _context = context;
            User = new UserRepo(_context);           
            RefreshToken = new RefreshTokenRepo(_context);
            EmailOtp = new EmailOtpRepo(_context);
            Category = new CategoryRepo(_context);
            Menu = new MenuRepo(_context);
            Cart = new CartRepo(_context);
            Voucher = new VoucherRepo(_context);
            VoucherRedemption = new VoucherRedemptionRepo(_context);
            Order = new OrderRepo(_context);
            Notification = new NotificationRepo(_context);
        }

        public IUserRepo User { get; }

        public IRefreshTokenRepo RefreshToken { get; }

        public IEmailOtpRepo EmailOtp { get; }

        public ICategoryRepo Category { get; }

        public IMenuRepo Menu  { get; }

        public ICartRepo Cart { get; }

        public IVoucherRepo Voucher { get; }

        public IVoucherRedemptionRepo VoucherRedemption { get; }

        public IOrderRepo Order { get; }

        public INotificationRepo Notification { get; }

        public async Task BeginTransactionAsync()
        {
            await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            await _context.Database.CommitTransactionAsync();
        }

        public async Task RollbackTransactionAsync()
        {
            await _context.Database.RollbackTransactionAsync();
        }

        public async Task SaveChangeAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
