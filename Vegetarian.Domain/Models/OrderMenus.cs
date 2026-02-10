using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Domain.Models
{
    public class OrderMenus
    {
        public Guid Id { get; set; }
        public Order Orders { get; set; } = null!;
        public Guid OrderId { get; set; }

        public Menu Menus { get; set; } = null!;
        public Guid MenuId { get; set; }

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
    }
}
