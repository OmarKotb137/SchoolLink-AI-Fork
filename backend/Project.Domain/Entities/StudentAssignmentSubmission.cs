namespace Project.Domain.Entities
{
    public class StudentAssignmentSubmission : BaseEntity
    {
        public int EnrollmentId { get; set; }
        public int AssignmentId { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public decimal? Score { get; set; }
        public decimal MaxScore { get; set; }
        public bool IsGraded { get; set; } = false;
        public string? AIFeedback { get; set; }

        // Navigation Properties
        public StudentEnrollment Enrollment { get; set; } = null!;
        public Assignment Assignment { get; set; } = null!;
        public ICollection<StudentAssignmentAnswer> Answers { get; set; } = new List<StudentAssignmentAnswer>();
    }
}
