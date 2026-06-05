using Project.Domain.Enums;

namespace Project.BLL.DTOs.Feedback;

public class LessonFeedbackDto
{
    public int Id { get; set; }
    public int EnrollmentId { get; set; }
    public int ClassSubjectTeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public DateOnly LessonDate { get; set; }
    public int Rating { get; set; }
    public LessonUnderstanding Understanding { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}
