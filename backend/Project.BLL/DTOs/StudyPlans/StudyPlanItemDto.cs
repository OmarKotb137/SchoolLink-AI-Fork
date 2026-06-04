using Project.Domain.Enums;

namespace Project.BLL.DTOs.StudyPlans;

public class StudyPlanItemDto
{
    public int Id { get; set; }
    public int StudyPlanId { get; set; }
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public SchoolDay DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? Topic { get; set; }
    public string? Notes { get; set; }
    public bool IsCompleted { get; set; }
}
