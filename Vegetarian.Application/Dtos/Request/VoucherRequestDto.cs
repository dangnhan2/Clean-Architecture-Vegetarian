using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Application.Dtos.Request
{
    public class VoucherRequestDto
    {
        public string Code { get; set; }
        public string DiscountType { get; set; }
        public int DiscountValue { get; set; }
        public int MaxDiscount { get; set; }
        public int MinOrderAmount { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public int? UsageLimit { get; set; }
        public int? PerUserLimit { get; set; }
        public bool IsActive { get; set; }
    }
}
