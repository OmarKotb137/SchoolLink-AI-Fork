namespace Project.BLL.DTOs.StudyPlans;

public class CreateStudyPlanItemRequest
{
    public int SubjectId { get; set; }
    public int DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? Topic { get; set; }
    public string? Notes { get; set; }
}
