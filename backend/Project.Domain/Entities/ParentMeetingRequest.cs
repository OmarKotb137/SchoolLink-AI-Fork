namespace Project.Domain.Entities;

public class ParentMeetingRequest : BaseEntity
{
    public int ParentId { get; set; }
    public int StudentId { get; set; }
    public int? TeacherId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime? PreferredDate { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public MeetingRequestStatus Status { get; set; } = MeetingRequestStatus.Pending;
    public string? Notes { get; set; }

    // Navigation Properties
    public User Parent { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public User? Teacher { get; set; }
}

public enum MeetingRequestStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Completed = 3
}
