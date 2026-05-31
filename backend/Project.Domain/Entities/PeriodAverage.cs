namespace SchoolLink.Domain.Entities
{
    public class PeriodAverage : BaseEntity
    {
        public int EnrollmentId { get; set; }
        public int PeriodId { get; set; }
        public decimal AvgScore { get; set; }
        public decimal MaxScore { get; set; }
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public StudentEnrollment Enrollment { get; set; } = null!;
        public EvaluationPeriod Period { get; set; } = null!;
    }
}
