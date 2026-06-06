using Project.Domain.Enums;

namespace Project.Domain.Entities
{
    public class ExamQuestion : BaseEntity
    {
        public int ExamId { get; set; }
        public int? GroupId { get; set; }
        public TemplateContentType DisplayType { get; set; } = TemplateContentType.None;
        public string? ContentText { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public QuestionType QuestionType { get; set; }
        public string? CorrectAnswer { get; set; }
        public string? ImageUrl { get; set; }
        public int DisplayOrder { get; set; }
        public decimal Points { get; set; }

        // Navigation Properties
        public Exam Exam { get; set; } = null!;
        public ExamQuestionGroup? Group { get; set; }
        public ICollection<ExamQuestionOption> Options { get; set; } = new List<ExamQuestionOption>();
    }
}
