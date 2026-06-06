namespace Project.BLL.AI.Models;

public class LessonPlanRequest
{
    public string Subject { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public int DurationMinutes { get; set; } = 45;
    public string GradeLevel { get; set; } = string.Empty;
    public string[]? LearningObjectives { get; set; }
}
