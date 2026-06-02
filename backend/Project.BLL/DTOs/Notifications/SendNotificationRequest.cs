using Project.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs.Notifications;

public class SendNotificationRequest
{
    [Required(ErrorMessage = "User ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid user ID")]
    public int UserId { get; set; }

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
