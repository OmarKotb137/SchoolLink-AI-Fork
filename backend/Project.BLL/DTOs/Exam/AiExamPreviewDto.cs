namespace Project.BLL.DTOs.Exam;

public class AiExamPreviewDto
{
    public int? ClassSubjectTeacherId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int? DurationMinutes { get; set; }
    public decimal TotalScore { get; set; }
    public int QuestionsCount { get; set; }
    public List<AiExamPreviewQuestionDto> StandaloneQuestions { get; set; } = new();
}

public class AiExamPreviewQuestionDto
{
    public string QuestionText { get; set; } = string.Empty;
    public int QuestionType { get; set; }
    public List<AiExamPreviewOptionDto>? Options { get; set; }
    public string? CorrectAnswer { get; set; }
    public decimal Points { get; set; }
    public int DisplayOrder { get; set; }
}

public class AiExamPreviewOptionDto
{
    public string OptionText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int DisplayOrder { get; set; }
}
