using Project.BLL.DTOs.Notifications;

namespace Project.BLL.Interfaces;

public interface INotificationPushService
{
    Task PushToUserAsync(int userId, NotificationDto notification);
    Task PushToUsersAsync(IEnumerable<int> userIds, NotificationDto notification);
}
