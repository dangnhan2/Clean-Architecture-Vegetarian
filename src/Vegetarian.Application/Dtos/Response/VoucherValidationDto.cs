using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Application.Dtos.Response
{
    public class VoucherValidationDto
    {
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
