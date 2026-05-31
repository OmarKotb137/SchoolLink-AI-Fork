namespace SchoolLink.Domain.Entities
{
    public class StudentEnrollment : BaseEntity
    {
        public int StudentId { get; set; }
        public int ClassId { get; set; }
        public int AcademicYearId { get; set; }
        public DateTime EnrolledAt { get; set; }
        public DateTime? LeftAt { get; set; }
        public string? TransferReason { get; set; }

        // Navigation Properties
        public Student Student { get; set; } = null!;
        public SchoolClass Class { get; set; } = null!;
        public AcademicYear AcademicYear { get; set; } = null!;
    }
}