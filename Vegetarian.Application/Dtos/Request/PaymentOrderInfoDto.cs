using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Application.Dtos.Request
{
    public class PaymentOrderInfoDto
    {
        public string CheckoutUrl { get; set; }
        public int OrderCode { get; set; }
    }
}
