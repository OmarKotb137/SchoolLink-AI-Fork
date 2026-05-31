using SchoolLink.Domain.Enums;

namespace SchoolLink.Domain.Entities
{
    public class ExamQuestion : BaseEntity
    {
        public int ExamId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public QuestionType QuestionType { get; set; }
        public string? CorrectAnswer { get; set; }
        public string? ImageUrl { get; set; }
        public int DisplayOrder { get; set; }
        public decimal Points { get; set; }

        // Navigation Properties
        public Exam Exam { get; set; } = null!;
        public ICollection<ExamQuestionOption> Options { get; set; } = new List<ExamQuestionOption>();
    }
}
