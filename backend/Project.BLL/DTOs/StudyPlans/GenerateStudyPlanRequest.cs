namespace Project.BLL.DTOs.StudyPlans;

public class GenerateStudyPlanRequest
{
    public int EnrollmentId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string? AIPromptSummary { get; set; }
}
