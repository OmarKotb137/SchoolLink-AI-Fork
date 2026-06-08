namespace Project.BLL.DTOs.StudyPlans;

public class StudyPlanSummaryDto
{
    public int Id { get; set; }
    public int EnrollmentId { get; set; }
    public bool GeneratedByAI { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsActive { get; set; }
    public int? RestDay { get; set; }
    public int TotalSessions { get; set; }
    public int CompletedSessions { get; set; }
    public double CompletionPercentage => TotalSessions > 0 ? Math.Round((double)CompletedSessions / TotalSessions * 100, 1) : 0;
    public DateTime CreatedAt { get; set; }
}
