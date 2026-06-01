using SchoolLink.Domain.Enums;

namespace Project.BLL.DTOs.Announcements;

public class CreateAnnouncementRequest
{
    public int AuthorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public UserRole? TargetRole { get; set; }
    public int? TargetClassId { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
