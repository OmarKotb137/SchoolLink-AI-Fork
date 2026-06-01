using Common.Results;
using Project.BLL.DTOs.Notifications;

namespace Project.BLL.Interfaces;

public interface INotificationService
{
    Task<OperationResult> SendNotificationAsync(SendNotificationRequest request);
    Task<OperationResult<IEnumerable<NotificationDto>>> GetNotificationsByUserAsync(int userId, bool onlyUnread);
    Task<OperationResult> MarkNotificationAsReadAsync(int notificationId, int userId);
    Task<OperationResult> MarkAllNotificationsAsReadAsync(int userId);
}
