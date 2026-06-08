namespace Project.BLL.DTOs.StudyPlans;

public class UpdateStudyPlanItemRequest
{
    public int Id { get; set; }
    public int StudyPlanId { get; set; }
    public int? SubjectId { get; set; }
    public int? DayOfWeek { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string? Topic { get; set; }
    public string? Notes { get; set; }
}
