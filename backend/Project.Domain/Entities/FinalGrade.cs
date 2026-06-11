namespace Project.Domain.Entities
{
    public class FinalGrade : BaseEntity
    {
        public int EnrollmentId { get; set; }
        public decimal PeriodAvgScore { get; set; }
        public decimal Assessment1Score { get; set; }
        public decimal Assessment2Score { get; set; }
        public decimal WrittenTotal { get; set; }
        public decimal FinalExamScore { get; set; }
        public decimal Total { get; set; }
        public decimal MaxTotal { get; set; }
        public bool IsPublished { get; set; } = false;
        public bool IsComplete { get; set; }

        // Navigation Properties
        public StudentEnrollment Enrollment { get; set; } = null!;
    }
}
