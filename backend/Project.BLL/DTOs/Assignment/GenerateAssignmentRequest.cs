using Project.Domain.Enums;

namespace Project.BLL.DTOs.Assignment;

public class GenerateAssignmentRequest
{
    public int ClassSubjectTeacherId { get; set; }
    public string Topic { get; set; } = string.Empty;
    public int QuestionCount { get; set; }
    public string? Difficulty { get; set; }
    public EvaluationCategory Category { get; set; }
}
