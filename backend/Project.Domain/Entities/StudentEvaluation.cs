namespace Project.Domain.Entities
{
    public class StudentEvaluation : BaseEntity
    {
        public int EnrollmentId { get; set; }
        public int EvaluationItemId { get; set; }
        public int PeriodId { get; set; }
        public decimal? Score { get; set; }
        public int EnteredById { get; set; }
        public DateTime EnteredAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public StudentEnrollment Enrollment { get; set; } = null!;
        public EvaluationItem EvaluationItem { get; set; } = null!;
        public EvaluationPeriod Period { get; set; } = null!;
        public User EnteredBy { get; set; } = null!;
    }
}
