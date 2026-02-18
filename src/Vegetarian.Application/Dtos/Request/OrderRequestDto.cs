using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Application.Dtos.Request
{
    public class OrderRequestDto
    {
        public Guid UserId { get; set; }
        public Guid? VoucherId { get; set; }
        public Guid AddressId { get; set; }
        public string? Note { get; set; }
        public string PaymentMethod { get; set; }
        public int TotalAmount { get; set; }
    }
}
