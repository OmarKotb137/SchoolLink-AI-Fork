using System.ComponentModel.DataAnnotations;
using Project.Domain.Enums;

namespace Project.BLL.DTOs.Announcements;

public class CreateAnnouncementRequest
{
    public int AuthorId { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 200 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Body is required")]
    [StringLength(5000, MinimumLength = 10, ErrorMessage = "Body must be between 10 and 5000 characters")]
    public string Body { get; set; } = string.Empty;

    public UserRole? TargetRole { get; set; }
    public int? TargetClassId { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime? ExpiresAt { get; set; }
}
