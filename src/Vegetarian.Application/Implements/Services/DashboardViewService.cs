using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Dtos.Response;
using Vegetarian.Application.Implements.Interface;
using Vegetarian.Domain.Enum;

namespace Vegetarian.Application.Implements.Services
{
    public class DashboardViewService : IDashboardViewService
    {
        private readonly IUnitOfWork _unitOfWork;
        public DashboardViewService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<DashboardViewDto> GetInfoAsync()
        {
            var today = DateTimeOffset.UtcNow.Date;
            var month = DateTimeOffset.UtcNow.Month;

            var totalOrdersToday = _unitOfWork.Order
                .GetAll()
                .Count(o => o.OrderDate.Date == today);

            var cancelledOrdersToday = _unitOfWork.Order
                .GetAll()
                .Count(o => o.Status == OrderStatus.Cancelled && o.OrderDate.Date == today);

            var paidOrders = _unitOfWork.Order
                .GetAll()
                .Where(o => o.Status == OrderStatus.Paid);

            var totalPaidOrdersToday = paidOrders
                .Count(o => o.Status == OrderStatus.Paid && o.OrderDate.Date == today);

            var totalMenuItems = _unitOfWork.Menu
                .GetAll()
                .Where(m => m.IsAvailable)
                .Count();

            var totalUsers = _unitOfWork.User
                .GetAll()
                .Where(u => !u.IsAdmin)
                .Count();

            var revenuePaidOrdersMonthly = _unitOfWork.Order
                .GetAll()
                .Where(u => u.Status == OrderStatus.Confirmed && u.OrderDate.Month == month)
                .Sum(o => o.TotalAmount);


            var totalAmount = paidOrders
                .Where(o => o.OrderDate.Date == today)
                .Sum(o => o.TotalAmount);

            // Get 5 the best menus
            var topSellingMenus = await _unitOfWork.Menu
                .GetAll()
                .OrderByDescending(m => m.SoldQuantity)
                .Take(5)
                .AsNoTracking()
                .Select(d => new TopMenuDto
                {
                    Name = d.Name,
                    ImageUrl = d.ImageUrl,
                    SoldQuantity = d.SoldQuantity
                })       
                .ToListAsync();

            // Get paid orders monthly
            var totalPaidOrdersMonthly = _unitOfWork.Order
                .GetAll()
                .Where(o => o.Status == OrderStatus.Confirmed && o.OrderDate.Month == month)
                .Count();

            // Get 5 the most spenders monthly
            var topBuyers = await _unitOfWork.User
                .GetTop5BuyersMonthly();

            var dashboardToDTO = new DashboardViewDto
            {
                TotalOrdersToday = totalOrdersToday,
                PaidOrdersToday = totalPaidOrdersToday,
                CancelledOrdersToday = cancelledOrdersToday,
                RevenueToday = totalAmount,
                TotalCustomers = totalUsers,
                TotalMenuItems = totalMenuItems,
                TopSellingMenus = topSellingMenus,
                RevenueMonthly = revenuePaidOrdersMonthly,
                TotalPaidOrdersMontly = totalPaidOrdersMonthly,
                TopBuyers = topBuyers
            };

            return dashboardToDTO;
        }
    }
}
