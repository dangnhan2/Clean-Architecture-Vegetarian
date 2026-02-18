using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Application.Dtos.Response
{
    public class DashboardViewDto
    {
        public int TotalOrdersToday { get; set; }
        public int PaidOrdersToday { get; set; }
        public int CancelledOrdersToday { get; set; }
        public decimal RevenueToday { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalMenuItems { get; set; }
        public decimal RevenueMonthly { get; set; }
        public int TotalPaidOrdersMontly { get; set; }
        public IEnumerable<TopMenuDto> TopSellingMenus { get; set; } = new List<TopMenuDto>();
        public IEnumerable<TopBuyerDto> TopBuyers { get; set; } = new List<TopBuyerDto>();
    }
}
