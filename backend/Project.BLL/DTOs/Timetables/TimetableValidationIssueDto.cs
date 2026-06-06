namespace Project.BLL.DTOs;

public class TimetableValidationIssueDto
{
    public string Severity { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? SlotId { get; set; }
    public string? DayOfWeek { get; set; }
    public int? PeriodNumber { get; set; }
    public int? ClassSubjectTeacherId { get; set; }
}
