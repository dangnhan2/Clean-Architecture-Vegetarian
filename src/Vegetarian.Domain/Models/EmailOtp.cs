using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Domain.Models
{
    public class EmailOtp
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public string Otp { get; set; }
        public bool IsUsed { get; set; }
        public DateTime ExpiredAt { get; set; } = DateTime.UtcNow.AddMinutes(5);
    }
}
