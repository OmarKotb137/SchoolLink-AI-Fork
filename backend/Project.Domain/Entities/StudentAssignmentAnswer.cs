namespace SchoolLink.Domain.Entities
{
    public class StudentAssignmentAnswer : BaseEntity
    {
        public int SubmissionId { get; set; }
        public int QuestionId { get; set; }
        public string? AnswerText { get; set; }
        public bool? IsCorrect { get; set; }
        public decimal PointsEarned { get; set; }
        public string? AIFeedback { get; set; }

        // Navigation Properties
        public StudentAssignmentSubmission Submission { get; set; } = null!;
        public AssignmentQuestion Question { get; set; } = null!;
    }
}