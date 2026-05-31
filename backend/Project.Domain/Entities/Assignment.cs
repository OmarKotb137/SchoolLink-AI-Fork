using SchoolLink.Domain.Enums;

namespace SchoolLink.Domain.Entities
{
    public class Assignment : BaseEntity
    {
        public int ClassSubjectTeacherId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal MaxScore { get; set; }
        public bool IsAutoGraded { get; set; } = false;
        public bool IsAIGenerated { get; set; } = false;
        public EvaluationCategory Category { get; set; }

        // Navigation Properties
        public ClassSubjectTeacher ClassSubjectTeacher { get; set; } = null!;
        public ICollection<AssignmentQuestion> Questions { get; set; } = new List<AssignmentQuestion>();
        public ICollection<StudentAssignmentSubmission> Submissions { get; set; } = new List<StudentAssignmentSubmission>();
    }
}