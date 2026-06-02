using Common.Results;
using Project.BLL.DTOs.Common;
using Project.BLL.DTOs.Notifications;

namespace Project.BLL.Interfaces;

public interface INotificationService
{
    Task<OperationResult> SendNotificationAsync(SendNotificationRequest request);
    Task<OperationResult<IEnumerable<NotificationDto>>> GetNotificationsByUserAsync(int userId, bool onlyUnread);
    Task<OperationResult<PagedResult<NotificationDto>>> GetNotificationsByUserPagedAsync(int userId, PaginationFilter filter);
    Task<OperationResult<NotificationDto>> GetNotificationByIdAsync(int notificationId, int userId);
    Task<OperationResult<int>> GetUnreadCountAsync(int userId);
    Task<OperationResult> MarkNotificationAsReadAsync(int notificationId, int userId);
    Task<OperationResult> MarkAllNotificationsAsReadAsync(int userId);
    Task<OperationResult> SendBulkNotificationAsync(SendBulkNotificationRequest request);
    Task<OperationResult> DeleteNotificationAsync(int notificationId, int userId);
    Task<OperationResult> DeleteBulkNotificationsAsync(List<int> notificationIds, int userId);
}
