using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Domain.Enum;

namespace Vegetarian.Domain.Models
{
    public class Order
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; }

        public Guid AddressId { get; set; }
        public Address Address { get; set; } 

        public string? Note { get; set; }
        public DateTimeOffset OrderDate { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? ExpiredAt { get; set; } 
        public OrderStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; }
        public int OrderCode { get; set; }

        public ICollection<OrderMenus> OrderMenus { get; set; } = new List<OrderMenus>();
        public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    }
}
