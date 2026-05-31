namespace SchoolLink.Domain.Entities
{
    public class StudentExamAttempt : BaseEntity
    {
        public int EnrollmentId { get; set; }
        public int ExamId { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SubmittedAt { get; set; }
        public decimal? Score { get; set; }
        public decimal TotalScore { get; set; }
        public bool IsGraded { get; set; } = false;

        // Navigation Properties
        public StudentEnrollment Enrollment { get; set; } = null!;
        public Exam Exam { get; set; } = null!;
        public ICollection<StudentExamAnswer> Answers { get; set; } = new List<StudentExamAnswer>();
    }
}
