using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs.AssignmentSubmission;

public class GradeSubmissionRequest
{
    [Required(ErrorMessage = "SubmissionId is required")]
    public int SubmissionId { get; set; }

    [Required(ErrorMessage = "AnswerGrades is required")]
    [MinLength(1, ErrorMessage = "At least one answer grade is required")]
    public List<AnswerGradeDto> AnswerGrades { get; set; } = new();

    [Range(0, double.MaxValue, ErrorMessage = "TotalScore must be non-negative")]
    public decimal TotalScore { get; set; }
}

public class AnswerGradeDto
{
    [Required(ErrorMessage = "QuestionId is required")]
    public int QuestionId { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "PointsEarned must be non-negative")]
    public decimal PointsEarned { get; set; }

    [MaxLength(2000)]
    public string? AiFeedback { get; set; }
}
