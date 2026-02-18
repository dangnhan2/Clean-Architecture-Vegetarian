using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Domain.Models;

namespace Vegetarian.Application.Abstractions.Persistence
{
    public interface IRefreshTokenRepo : IGenericRepo<RefreshToken>
    {
        public Task<RefreshToken?> GetTokenByRefreshToken(string refreshToken);
    }
}
