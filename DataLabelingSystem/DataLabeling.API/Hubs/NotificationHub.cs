using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace DataLabeling.API.Hubs
{
    public class NotificationHub : Hub
    {
        public async Task JoinProjectGroup(string projectId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Project_{projectId}");
        }

        public async Task JoinAdminGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
        }
    }
}