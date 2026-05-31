using SchoolLink.Domain.Enums;

namespace SchoolLink.Domain.Entities
{
    public class AssignmentQuestion : BaseEntity
    {
        public int AssignmentId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public QuestionType QuestionType { get; set; }
        public string? ImageUrl { get; set; }
        public string? CorrectAnswer { get; set; }
        public int DisplayOrder { get; set; }
        public decimal Points { get; set; }

        // Navigation Properties
        public Assignment Assignment { get; set; } = null!;
        public ICollection<AssignmentQuestionOption> Options { get; set; } = new List<AssignmentQuestionOption>();
    }
}