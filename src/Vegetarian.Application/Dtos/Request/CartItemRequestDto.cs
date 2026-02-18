using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Application.Dtos.Request
{
    public class CartItemRequestDto
    {
        public Guid MenuId { get; set; }
        public int Quantity { get; set; }
        public int UnitPrice { get; set; }
    }
}
