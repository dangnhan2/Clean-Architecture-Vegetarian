using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Domain.Enum;

namespace Vegetarian.Application.Dtos.Request
{
    public class CancelOrderRequestDto
    {   
        public Guid UserId { get; set; }
        public string Reason { get; set; } = null!;
        public PaymentMethod PaymentMethod { get; set; }
        public string? BankBin { get; set; } 
        public string? BankAccountNumber { get; set; }
    }
}
