using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Dtos.Request;
using Vegetarian.Application.Dtos.Response;

namespace Vegetarian.Application.Implements.Interface
{
    public interface ICartService
    {
        public Task AddToCartAsync(CartRequestDto request);
        public Task<CartDto> GetCartByCustomer(Guid userId);
    }
}
