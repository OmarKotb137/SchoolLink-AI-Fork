using Project.Domain.Enums;

namespace Project.BLL.DTOs.Announcements;

public class AnnouncementDto
{
    public int Id { get; set; }
    public int AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public UserRole? TargetRole { get; set; }
    public int? TargetClassId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
