using DataLabeling.API.Hubs;
using DataLabeling.Core.DTOs;
using DataLabeling.Core.Interfaces;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace DataLabeling.API.Services
{
    public class SignalRNotificationService : INotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public SignalRNotificationService(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyUserAsync(string userId, string message)
        {
            await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", message);
        }

        public async Task NotifyProjectUpdateAsync(int projectId)
        {
            await _hubContext.Clients.Group($"Project_{projectId}").SendAsync("UpdateDashboard");
        }

        public async Task NotifyNewLogAsync(ActivityLogDto logDto)
        {
            await _hubContext.Clients.Group("Admins").SendAsync("ReceiveNewLog", logDto);
        }

        public async Task NotifyProjectListUpdateAsync()
        {

            await _hubContext.Clients.All.SendAsync("RefreshProjectList");
        }
    }
}