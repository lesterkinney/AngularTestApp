using API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace API.Services.SignalR
{
    [Authorize]
    public class PresenceHub : Hub
    {
        private readonly PresenceTracker tracker;

        public PresenceHub(PresenceTracker tracker)
        {
            this.tracker = tracker;
        }

        public override async Task OnConnectedAsync()
        {
            var isOnline = await this.tracker.UserConnected(Context.User.GetUserName(), Context.ConnectionId);
            if(isOnline)
            {
                await Clients.Others.SendAsync("UserIsOnline", Context.User.GetUserName());
            }
            

            var currentUsers = await this.tracker.GetOnlineUsers();
            await Clients.Caller.SendAsync("GetOnlineUsers", currentUsers);
        }

        public override async Task OnDisconnectedAsync(Exception ex)
        {
            var isOffline = await this.tracker.UserDisconnected(Context.User.GetUserName(), Context.ConnectionId);
            if(isOffline)
            {
                await Clients.Others.SendAsync("UserIsOffline", Context.User.GetUserName());
            }
            

            await base.OnDisconnectedAsync(ex);
        }
    }
}
