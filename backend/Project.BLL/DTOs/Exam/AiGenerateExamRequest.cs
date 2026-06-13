using Project.Domain.Enums;

namespace Project.BLL.DTOs.Exam;

public class AiGenerateExamRequest
{
    public int? ClassSubjectTeacherId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? DurationMinutes { get; set; }
    public decimal TotalScore { get; set; }
    public EvaluationCategory Category { get; set; }
    public Dictionary<int, int> QuestionCounts { get; set; } = new();
    public string? Topic { get; set; }
    public int? UnitId { get; set; }
    public List<int> LessonIds { get; set; } = new();
}
