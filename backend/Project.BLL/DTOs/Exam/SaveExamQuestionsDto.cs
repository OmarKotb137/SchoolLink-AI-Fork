using Project.Domain.Enums;

namespace Project.BLL.DTOs.Exam;

public class SaveExamQuestionsDto
{
    public Guid Uid { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? DurationMinutes { get; set; }
    public decimal TotalScore { get; set; }
    public int? ClassSubjectTeacherId { get; set; }
    public List<SaveQuestionDto> Questions { get; set; } = new();
}

public class SaveQuestionDto
{
    public int Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? CorrectAnswer { get; set; }
    public decimal Points { get; set; }
    public int DisplayOrder { get; set; }
    public List<SaveOptionDto> Options { get; set; } = new();
}

public class SaveOptionDto
{
    public int Id { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int DisplayOrder { get; set; }
}
