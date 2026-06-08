namespace Project.BLL.DTOs.StudyPlans;

public class StudyPlanDto
{
    public int Id { get; set; }
    public int EnrollmentId { get; set; }
    public bool GeneratedByAI { get; set; }
    public string? AIPromptSummary { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsActive { get; set; }
    public int? RestDay { get; set; }
    public List<StudyPlanItemDto> Items { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
