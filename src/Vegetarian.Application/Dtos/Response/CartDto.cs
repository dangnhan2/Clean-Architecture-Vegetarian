using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Application.Dtos.Response
{
    public class CartDto
    {
        public Guid Id { set; get; }
        public Guid UserId { get; set; }
        public ICollection<CartItemDto> Items { get; set; } = new List<CartItemDto>();
    }
}
