using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Application.Dtos.Request
{
    public class ValidationVoucherRequestDto
    {
        public Guid UserId { get; set; }
        public Guid VoucherId { get; set; }
    }
}
