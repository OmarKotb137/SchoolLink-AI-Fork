using SchoolLink.Domain.Enums;

namespace Project.BLL.DTOs.Notifications;

public class SendBulkNotificationRequest
{
    public List<int> UserIds { get; set; } = new();
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public string? DataJson { get; set; }
}
