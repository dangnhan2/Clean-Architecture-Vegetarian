using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Dtos.Request;
using Vegetarian.Application.Dtos.Response;

namespace Vegetarian.Application.Implements.Interface
{
    public interface INotificationService
    {
        public Task<UnreadNotificationDto> GetUnReadNotificationsByAdmin(Guid adminId);
        public Task<IEnumerable<NotificationDto>> GetNotificationsByAdmin(Guid adminId);
        public Task MarkAsReadAsync(MarkNotificationRequestDto notificationIds);
        public Task DeleteAsync(Guid id);
    }
}
