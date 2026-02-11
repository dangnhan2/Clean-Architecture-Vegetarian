using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application;
using Vegetarian.Application.Dtos.Response;
using Vegetarian.Application.Helper;
using Vegetarian.Application.SignalR;
using Vegetarian.Domain.Models;
using Vegetarian.Infrastructure.Services.SignalR.SignalR_Hub;

namespace Vegetarian.Infrastructure.Services.SignalR.SignalR_Sender
{
    public class NotificationSenderService : INotificationSenderRepo
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly UserManager<User> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public NotificationSenderService(
            IHubContext<NotificationHub> hubContext,
            UserManager<User> userManager,
            IUnitOfWork unitOfWork)
        {
            _hubContext = hubContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }


        public async Task NotifyAdminWhenMenuRatedAsync(string? comment, string userName, string menuName, Guid menuId)
        {
            var users = await _userManager.GetUsersInRoleAsync("Admin");
            List<Notification> notifications = new List<Notification>();

            foreach (var user in users)
            {
                var newNotification = MappingNotificationWhenMenuRated(user.Id, comment, userName, menuName, menuId);
                notifications.Add(newNotification);

                // send notification to group admins
                await _hubContext.Clients.Group("Admins")
                    .SendAsync("RatineMenu", new NotificationDto
                    {
                        Id = newNotification.Id,
                        Title = $"Khách hàng {userName} đã đánh giá món {menuName}",
                        Message = $"{comment}",
                        Type = "Rating",
                        IsRead = false,
                        Data = $"{menuId}",
                        CreatedAt = DateTimeOffset.UtcNow.FormatDateTimeOffset()
                    });
            }

            await _unitOfWork.Notification.AddRangeAsync(notifications);
            await _unitOfWork.SaveChangeAsync();
        }

        public async Task NotifyAdminWhenNewOrderCreatedAsync(int orderCode, decimal totalAmount)
        {
            var users = await _userManager.GetUsersInRoleAsync("Admin");
            List<Notification> notifications = new List<Notification>();

            foreach (var user in users)
            {
                var newNotification = MappingNotificationWhenNewOrderCreated(user.Id, orderCode);
                notifications.Add(newNotification);

                // send notification to group admins
                await _hubContext.Clients.Group("Admins")
                    .SendAsync("NewOrder", new NotificationDto
                    {
                        Id = newNotification.Id,
                        Title = "Bạn có đơn hàng mới",
                        Message = $"Đơn hàng #{orderCode} vừa được tạo",
                        Type = "Order",
                        Data = $"Đơn hàng #{orderCode} thanh toán thành công. Tổng giá trị ${totalAmount}",
                        IsRead = false,
                        CreatedAt = DateTimeOffset.UtcNow.FormatDateTimeOffset()
                    });
            }

            await _unitOfWork.Notification.AddRangeAsync(notifications);
            await _unitOfWork.SaveChangeAsync();
        }


        #region        
        private Notification MappingNotificationWhenNewOrderCreated(Guid userId, int orderCode)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Tiltle = "Bạn có đơn hàng mới",
                Message = $"Đơn hàng #{orderCode} vừa đặt thành công",
                Type = "Order",
                Data = "",
                IsRead = false
            };

            return notification;
        }

        private Notification MappingNotificationWhenMenuRated(Guid userId, string? comment, string userName, string menuName, Guid menuId)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Tiltle = $"Khách hàng {userName} đã đánh giá món {menuName}",
                Message = $"{comment}",
                Type = "Rating",
                Data = $"{menuId}",
                IsRead = false
            };

            return notification;
        }
        #endregion
    }
}
