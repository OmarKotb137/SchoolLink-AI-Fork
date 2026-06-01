using SchoolLink.Domain.Enums;

namespace Project.BLL.DTOs.Notifications;

public class SendNotificationRequest
{
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public string? DataJson { get; set; }
}
