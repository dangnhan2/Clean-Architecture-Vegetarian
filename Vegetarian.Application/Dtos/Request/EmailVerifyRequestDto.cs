using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Application.Dtos.Request
{
    public class EmailVerifyRequestDto
    {
        public string Email { get; set; }
        public string Otp { get; set; }
    }
}
