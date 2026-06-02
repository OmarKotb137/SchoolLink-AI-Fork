namespace Project.Domain.Entities
{
    public class StudentExamAnswer : BaseEntity
    {
        public int AttemptId { get; set; }
        public int QuestionId { get; set; }
        public string? AnswerText { get; set; }
        public int? SelectedOptionId { get; set; }
        public bool? BooleanAnswer { get; set; }
        public bool? IsCorrect { get; set; }
        public decimal PointsEarned { get; set; }
        public string? AIFeedback { get; set; }

        // Navigation Properties
        public StudentExamAttempt Attempt { get; set; } = null!;
        public ExamQuestion Question { get; set; } = null!;
        public ExamQuestionOption? SelectedOption { get; set; }
    }
}
