using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application;
using Vegetarian.Application.Repositories;
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
        }

        public IUserRepo User { get; }

        public IRefreshTokenRepo RefreshToken { get; }

        public IEmailOtpRepo EmailOtp { get; }

        public Task BeginTransactionAsync()
        {
            throw new NotImplementedException();
        }

        public Task CommitTransactionAsync()
        {
            throw new NotImplementedException();
        }

        public Task RollbackTransactionAsync()
        {
            throw new NotImplementedException();
        }

        public Task SaveChangeAsync()
        {
            throw new NotImplementedException();
        }
    }
}
