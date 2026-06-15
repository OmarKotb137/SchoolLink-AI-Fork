using Project.Domain.Enums;

namespace Project.Domain.Entities
{
    public class Exam : BaseEntity
    {
        public Guid Uid { get; set; } = Guid.NewGuid();
        public int? ClassSubjectTeacherId { get; set; }
        public int? SubjectId { get; set; }
        public int GradeLevelId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? DurationMinutes { get; set; }
        public decimal TotalScore { get; set; }
        public bool IsAIGenerated { get; set; } = false;
        public bool IsPublished { get; set; } = false;
        public EvaluationCategory Category { get; set; }

        // Navigation Properties
        public ClassSubjectTeacher? ClassSubjectTeacher { get; set; }
        public Subject? Subject { get; set; }
        public GradeLevel GradeLevel { get; set; } = null!;
        public ICollection<ExamQuestionGroup> Groups { get; set; } = new List<ExamQuestionGroup>();
        public ICollection<ExamQuestion> Questions { get; set; } = new List<ExamQuestion>();
        public ICollection<ExamQuestionBankItem> QuestionBankLinks { get; set; } = new List<ExamQuestionBankItem>();
        public ICollection<StudentExamAttempt> Attempts { get; set; } = new List<StudentExamAttempt>();
    }
}
