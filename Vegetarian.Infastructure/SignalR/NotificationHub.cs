using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Infrastructure.SignalR
{
    public sealed class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            // get role of user from claims
            var roles = Context.User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value);

            // check role if user is admin
            if (roles.Contains("Admin"))
                // if user is admin to admin group
                await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");

            await Groups.AddToGroupAsync(Context.ConnectionId, $"User: {userId}");
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            return base.OnDisconnectedAsync(exception);
        }
    }
}
