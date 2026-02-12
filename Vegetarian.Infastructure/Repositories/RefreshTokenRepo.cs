using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Abstractions.Persistence;
using Vegetarian.Application.Helper;
using Vegetarian.Domain.Models;
using Vegetarian.Infrastructure.Data;

namespace Vegetarian.Infrastructure.Repositories
{
    public class RefreshTokenRepo : GenericRepo<RefreshToken>, IRefreshTokenRepo
    {
        private readonly VegetarianDbContext _context;
        public RefreshTokenRepo(VegetarianDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<RefreshToken?> GetTokenByRefreshToken(string refreshToken)
        {
            return await _context.RefreshToken
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == refreshToken.HashToken());
        }
    }
}
