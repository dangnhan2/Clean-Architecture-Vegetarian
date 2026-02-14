using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Application.Abstractions.Notifications
{
    public interface INotificationSender
    {
        public Task NotifyAdminWhenNewOrderCreatedAsync(int orderCode, decimal totalAmount);
        public Task NotifyAdminWhenMenuRatedAsync(string? comment, string userName, string menuName, Guid menuId);
        public Task NotifyCustomerWhenOrderConfirmedAsync(Guid userId, Guid orderId, int orderCode);

    }
}
