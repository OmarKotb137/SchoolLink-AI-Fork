namespace Project.BLL.DTOs;

public class TimetableValidationResultDto
{
    public int TimetableId { get; set; }
    public bool CanActivate { get; set; }
    public int TotalSlots { get; set; }
    public int LessonSlots { get; set; }
    public int BreakSlots { get; set; }
    public int MissingAssignmentsCount { get; set; }
    public int OverScheduledAssignmentsCount { get; set; }
    public List<TimetableValidationIssueDto> Errors { get; set; } = new();
    public List<TimetableValidationIssueDto> Warnings { get; set; } = new();
}
