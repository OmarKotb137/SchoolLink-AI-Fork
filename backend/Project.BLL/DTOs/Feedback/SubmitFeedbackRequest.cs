using Project.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs.Feedback;

public class SubmitFeedbackRequest
{
    public int EnrollmentId { get; set; }
    public int ClassSubjectTeacherId { get; set; }
    public DateOnly LessonDate { get; set; }

    [Range(1, 5, ErrorMessage = "التقييم يجب أن يكون بين 1 و 5")]
    public int Rating { get; set; }

    public LessonUnderstanding Understanding { get; set; }
    public string? Comment { get; set; }
}
