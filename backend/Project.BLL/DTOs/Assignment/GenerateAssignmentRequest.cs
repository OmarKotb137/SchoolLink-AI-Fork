using System.ComponentModel.DataAnnotations;
using Project.Domain.Enums;

namespace Project.BLL.DTOs.Assignment;

public class GenerateAssignmentRequest
{
    [Required(ErrorMessage = "ClassSubjectTeacherId is required")]
    public int ClassSubjectTeacherId { get; set; }

    [Required(ErrorMessage = "Topic is required")]
    [MinLength(2, ErrorMessage = "Topic must be at least 2 characters")]
    [MaxLength(500, ErrorMessage = "Topic must not exceed 500 characters")]
    public string Topic { get; set; } = string.Empty;

    [Range(1, 50, ErrorMessage = "QuestionCount must be between 1 and 50")]
    public int QuestionCount { get; set; }

    [MaxLength(50)]
    public string? Difficulty { get; set; }

    [Required(ErrorMessage = "Category is required")]
    public EvaluationCategory Category { get; set; }
}
