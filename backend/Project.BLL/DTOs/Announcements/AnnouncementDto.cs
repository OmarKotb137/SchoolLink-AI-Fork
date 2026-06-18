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
    public AnnouncementType? Category { get; set; }
    public int? TargetGradeLevelId { get; set; }
    public bool IsForAllUsers { get; set; }
    public bool IsForAllStudents { get; set; }
    public bool IsForAllParents { get; set; }
    public bool IsForAllTeachers { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TargetedUserCount { get; set; }
}
