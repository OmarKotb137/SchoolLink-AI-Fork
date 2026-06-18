namespace Project.BLL.DTOs.Meetings;

public class CreateMeetingRequest
{
    public int ParentId { get; set; }
    public int StudentId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime? PreferredDate { get; set; }
    public string? Notes { get; set; }
}
