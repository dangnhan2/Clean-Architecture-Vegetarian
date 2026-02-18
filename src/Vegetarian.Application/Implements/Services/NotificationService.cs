using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Dtos.Request;
using Vegetarian.Application.Dtos.Response;
using Vegetarian.Application.Helper;
using Vegetarian.Application.Implements.Interface;

namespace Vegetarian.Application.Implements.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        public NotificationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task DeleteAsync(Guid id)
        {
            var existNotification = await _unitOfWork.Notification.GetByIdAsync(id);

            if (existNotification == null) throw new KeyNotFoundException("Thông báo không tồn tại");

            _unitOfWork.Notification.Remove(existNotification);

            await _unitOfWork.SaveChangeAsync();
        }

        public async Task<IEnumerable<NotificationDto>> GetNotificationsAsync(Guid userId)
        {
            var notifications = _unitOfWork.Notification
                .GetAll().Where(n => n.UserId == userId);

            var notificationToDto = await notifications
                .OrderByDescending(n => n.CreatedAt)              
                .AsNoTracking()
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Tiltle,
                    Message = n.Message,
                    Type = n.Type,
                    Data = n.Data,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt.FormatDateTime()
                }).ToListAsync();

            return notificationToDto;
        }

        public async Task<UnreadNotificationDto> GetUnReadNotificationsAsync(Guid userId)
        {
            var notifications = _unitOfWork.Notification
               .GetAll()
               .Where(n => !n.IsRead && n.UserId == userId)
               .Take(99);

            var notificationToDto = await notifications
                .OrderByDescending(n => n.CreatedAt)
                .AsNoTracking()
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Tiltle,
                    Message = n.Message,
                    Type = n.Type,
                    Data = n.Data,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt.FormatDateTime()
                }).ToListAsync();

            return new UnreadNotificationDto
            {
                Total = await notifications.CountAsync(),
                UnreadNotifications = notificationToDto
            };
        }

        public async Task MarkAsReadAsync(MarkNotificationRequestDto notificationIds)
        {
            foreach (var id in notificationIds.NotificationIds)
            {
                var existNotification = await _unitOfWork.Notification.GetByIdAsync(id);

                if (existNotification == null) continue;

                existNotification.IsRead = true;
            }
            await _unitOfWork.SaveChangeAsync();
        }
    }
}
