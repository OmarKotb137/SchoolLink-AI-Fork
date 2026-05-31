namespace SchoolLink.Domain.Entities
{
    public class ExamQuestionOption : BaseEntity
    {
        public int QuestionId { get; set; }
        public string OptionText { get; set; } = string.Empty;
        public bool IsCorrect { get; set; } = false;
        public int DisplayOrder { get; set; }

        // Navigation Properties
        public ExamQuestion Question { get; set; } = null!;
    }
}
