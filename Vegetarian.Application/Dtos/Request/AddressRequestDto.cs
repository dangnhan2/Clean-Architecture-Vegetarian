using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Application.Dtos.Request
{
    public class AddressRequestDto
    {
        public Guid UserId { get; set; }
        public string Address { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Province { get; set; }
        public string District { get; set; }
        public bool IsDefault { get; set; }
    }
}
