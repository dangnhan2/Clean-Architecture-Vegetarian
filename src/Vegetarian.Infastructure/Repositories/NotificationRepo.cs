using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Abstractions.Persistence;
using Vegetarian.Domain.Models;
using Vegetarian.Infrastructure.Data;

namespace Vegetarian.Infrastructure.Repositories
{
    public class NotificationRepo : GenericRepo<Notification>, INotificationRepo
    {
        private readonly VegetarianDbContext _context;
        public NotificationRepo(VegetarianDbContext context) : base(context)
        {
            _context = context;
        }

        public void DeleteListOfNotification(List<Guid> notificationIds)
        {
            var notifications = _context.Notification
                 .Where(n => notificationIds.Contains(n.Id));

            _context.Notification.RemoveRange(notifications);
        }
    }
}
