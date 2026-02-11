using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Application.Dtos.Request
{
    public class CartRequestDto
    {
        public Guid UserId { get; set; }
        public ICollection<CartItemRequestDto> CartItems { get; set; } = new List<CartItemRequestDto>();
    }
}
