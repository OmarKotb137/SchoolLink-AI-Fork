using SchoolLink.Domain.Enums;

namespace SchoolLink.Domain.Entities
{
    public class LessonFeedback : BaseEntity
    {
        public int EnrollmentId { get; set; }
        public int ClassSubjectTeacherId { get; set; }
        public DateTime LessonDate { get; set; }
        public int Rating { get; set; }
        public LessonUnderstanding Understanding { get; set; }
        public string? Comment { get; set; }

        // Navigation Properties
        public StudentEnrollment Enrollment { get; set; } = null!;
        public ClassSubjectTeacher ClassSubjectTeacher { get; set; } = null!;
    }
}