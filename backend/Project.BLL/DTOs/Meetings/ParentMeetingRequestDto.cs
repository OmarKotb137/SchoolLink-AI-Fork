using Project.Domain.Entities;

namespace Project.BLL.DTOs.Meetings;

public class ParentMeetingRequestDto
{
    public int Id { get; set; }
    public int ParentId { get; set; }
    public string ParentName { get; set; } = string.Empty;
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int? TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime? PreferredDate { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public MeetingRequestStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
