using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Application.Dtos.Request
{
    public class UserRequestDto
    {
        public string PhoneNumber { get; set; }
        public IFormFile? Avatar { get; set; }
    }
}
