using DataLabeling.Core.DTOs;
using System.Threading.Tasks;

namespace DataLabeling.Core.Interfaces
{
    public interface INotificationService
    {
        Task NotifyUserAsync(string userId, string message);
        Task NotifyProjectUpdateAsync(int projectId);

        Task NotifyNewLogAsync(ActivityLogDto logDto);

        Task NotifyProjectListUpdateAsync();
    }
}