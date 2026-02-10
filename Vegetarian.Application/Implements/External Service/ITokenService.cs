using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Dtos.Response;
using Vegetarian.Domain.Models;
using Microsoft.AspNetCore.Http;

namespace Vegetarian.Application.Services.External_Service
{
    public interface ITokenService
    {
        public Task<AuthResponse> GenerateToken(User user, HttpContext context);
        public Task<AuthResponse> GenerateRefreshToken(HttpContext context);
    }
}
