using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Domain.Enum;

namespace Vegetarian.Application.Dtos.Response
{
    public class OrderDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string OrderDate { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Note { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public decimal TotalAmount { get; set; }
        public int OrderCode { get; set; }
        public string PaymentMethod { get; set; }
        public ICollection<OrderMenuDto> Menus { get; set; } = new List<OrderMenuDto>();
    }
}
