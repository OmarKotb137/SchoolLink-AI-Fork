using Project.Domain.Enums;

namespace Project.BLL.DTOs.StudentExams;

public class StudentExamListItemDto
{
    public int ExamId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public int? DurationMinutes { get; set; }
    public decimal TotalScore { get; set; }
    public int QuestionsCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? AttemptId { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public bool IsGraded { get; set; }
    public bool IsResultPublished { get; set; }
    public decimal? Score { get; set; }
}

public class StudentExamDetailsDto
{
    public int ExamId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? DurationMinutes { get; set; }
    public decimal TotalScore { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<StudentExamQuestionDto> Questions { get; set; } = new();
}

public class StudentExamQuestionDto
{
    public int Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public string? ContentText { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Points { get; set; }
    public int DisplayOrder { get; set; }
    public List<StudentExamQuestionOptionDto> Options { get; set; } = new();
}

public class StudentExamQuestionOptionDto
{
    public int Id { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}

public class StudentExamAttemptStartedDto
{
    public int AttemptId { get; set; }
    public int ExamId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset ServerNow { get; set; }
    public int? DurationMinutes { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
}

// DTO لحفظ إجابة واحدة تلقائياً أثناء الامتحان (Auto-Save)
public class SaveAnswerProgressDto
{
    public int     QuestionId       { get; set; }
    public string? AnswerText       { get; set; }
    public int?    SelectedOptionId { get; set; }
    public bool?   BooleanAnswer    { get; set; }
}

public class SubmitStudentExamAttemptDto
{
    public List<StudentExamAnswerPayloadDto> Answers { get; set; } = new();
}

public class StudentExamAnswerPayloadDto
{
    public int QuestionId { get; set; }
    public string? AnswerText { get; set; }
    public int? SelectedOptionId { get; set; }
    public bool? BooleanAnswer { get; set; }
}

public class StudentExamAttemptResultDto
{
    public int AttemptId { get; set; }
    public bool IsSubmitted { get; set; }
    public bool IsGraded { get; set; }
    public bool IsResultPublished { get; set; }
    public decimal? Score { get; set; }
    public decimal TotalScore { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<StudentExamResultAnswerDto> Answers { get; set; } = new();
}

public class StudentExamResultAnswerDto
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? AnswerText { get; set; }
    public int? SelectedOptionId { get; set; }
    public bool? BooleanAnswer { get; set; }
    public bool? IsCorrect { get; set; }
    public string? CorrectAnswerText { get; set; }
    public decimal PointsEarned { get; set; }
    public decimal QuestionPoints { get; set; }
    public string? AIFeedback { get; set; }
}
