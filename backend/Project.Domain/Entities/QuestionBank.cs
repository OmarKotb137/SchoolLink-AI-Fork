using Project.Domain.Enums;

namespace Project.Domain.Entities
{
    public class QuestionBank : BaseEntity
    {
        public string QuestionText { get; set; } = string.Empty;
        public QuestionType QuestionType { get; set; }
        public string? CorrectAnswer { get; set; }
        public string? OptionsJson { get; set; }
        public int SubjectId { get; set; }
        public int GradeLevelId { get; set; }
        public int? SourceExamId { get; set; }
        public int UsageCount { get; set; } = 0;

        // Navigation Properties
        public Subject Subject { get; set; } = null!;
        public GradeLevel GradeLevel { get; set; } = null!;
        public Exam? SourceExam { get; set; }
        public ICollection<ExamQuestionBankItem> ExamLinks { get; set; } = new List<ExamQuestionBankItem>();
    }
}
