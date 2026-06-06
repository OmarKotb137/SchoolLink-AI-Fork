namespace Project.BLL.AI.Models;

public class AiQuestionRequest
{
    public string Subject { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string GradeLevel { get; set; } = string.Empty;
    public string Difficulty { get; set; } = "medium";
    public string QuestionText { get; set; } = string.Empty;
}
