namespace SchoolLink.Domain.Entities
{
    public class AssignmentQuestionOption : BaseEntity
    {
        public int QuestionId { get; set; }
        public string OptionText { get; set; } = string.Empty;
        public bool IsCorrect { get; set; } = false;
        public int DisplayOrder { get; set; }

        // Navigation Properties
        public AssignmentQuestion Question { get; set; } = null!;
    }
}
