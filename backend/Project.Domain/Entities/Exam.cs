using SchoolLink.Domain.Enums;

namespace SchoolLink.Domain.Entities
{
    public class Exam : BaseEntity
    {
        public int ClassSubjectTeacherId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? DurationMinutes { get; set; }
        public decimal TotalScore { get; set; }
        public bool IsAIGenerated { get; set; } = false;
        public bool IsPublished { get; set; } = false;
        public EvaluationCategory Category { get; set; }

        // Navigation Properties
        public ClassSubjectTeacher ClassSubjectTeacher { get; set; } = null!;
        public ICollection<ExamQuestion> Questions { get; set; } = new List<ExamQuestion>();
        public ICollection<StudentExamAttempt> Attempts { get; set; } = new List<StudentExamAttempt>();
    }
}
