using System.ComponentModel.DataAnnotations;
using Project.Domain.Enums;

namespace Project.BLL.DTOs.Notifications;

public class SendBulkNotificationRequest
{
    [Required(ErrorMessage = "At least one user ID is required")]
    [MinLength(1, ErrorMessage = "At least one user ID is required")]
    public List<int> UserIds { get; set; } = new();

    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 200 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Body is required")]
    [StringLength(2000, MinimumLength = 2, ErrorMessage = "Body must be between 2 and 2000 characters")]
    public string Body { get; set; } = string.Empty;

    [Required(ErrorMessage = "Notification type is required")]
    public NotificationType Type { get; set; }

    public string? DataJson { get; set; }
}
