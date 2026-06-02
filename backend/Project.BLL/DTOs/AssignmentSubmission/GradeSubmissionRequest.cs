namespace Project.BLL.DTOs.AssignmentSubmission;

public class GradeSubmissionRequest
{
    public int SubmissionId { get; set; }
    public List<AnswerGradeDto> AnswerGrades { get; set; } = new();
    public decimal TotalScore { get; set; }
}

public class AnswerGradeDto
{
    public int QuestionId { get; set; }
    public decimal PointsEarned { get; set; }
    public string? AiFeedback { get; set; }
}
