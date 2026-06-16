using Project.Domain.Enums;

namespace Project.BLL.DTOs.StudentAssignments;

public class StudentAssignmentListItemDto
{
    public int AssignmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public decimal MaxScore { get; set; }
    public int QuestionsCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? SubmissionId { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public bool IsGraded { get; set; }
    public decimal? Score { get; set; }
}

public class StudentAssignmentDetailsDto
{
    public int AssignmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public decimal MaxScore { get; set; }
    public bool IsAutoGraded { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? SubmissionId { get; set; }
    public List<StudentAssignmentQuestionDto> Questions { get; set; } = new();
}

public class StudentAssignmentQuestionDto
{
    public int Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Points { get; set; }
    public int DisplayOrder { get; set; }
    public List<StudentAssignmentQuestionOptionDto> Options { get; set; } = new();
}

public class StudentAssignmentQuestionOptionDto
{
    public int Id { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}

public class SubmitStudentAssignmentDto
{
    public List<StudentAssignmentAnswerPayloadDto> Answers { get; set; } = new();
}

public class StudentAssignmentAnswerPayloadDto
{
    public int QuestionId { get; set; }
    public string? AnswerText { get; set; }
    public int? SelectedOptionId { get; set; }
    public bool? BooleanAnswer { get; set; }
}

public class StudentAssignmentSubmissionResultDto
{
    public int SubmissionId { get; set; }
    public int AssignmentId { get; set; }
    public bool IsSubmitted { get; set; }
    public bool IsGraded { get; set; }
    public decimal? Score { get; set; }
    public decimal MaxScore { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<StudentAssignmentResultAnswerDto> Answers { get; set; } = new();
}

public class StudentAssignmentResultAnswerDto
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? AnswerText { get; set; }
    public int? SelectedOptionId { get; set; }
    public bool? BooleanAnswer { get; set; }
    public bool? IsCorrect { get; set; }
    public decimal PointsEarned { get; set; }
    public decimal QuestionPoints { get; set; }
    public string? AIFeedback { get; set; }
}
