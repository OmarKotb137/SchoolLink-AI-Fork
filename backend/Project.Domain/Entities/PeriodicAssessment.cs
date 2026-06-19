using Project.Domain.Enums;

namespace Project.Domain.Entities
{
    public class PeriodicAssessment : BaseEntity
    {
        public int EnrollmentId { get; set; }
        public int? SubjectId { get; set; }
        public PeriodicAssessmentType AssessmentType { get; set; }
        public decimal Score { get; set; }
        public decimal MaxScore { get; set; }
        public DateOnly? AssessmentDate { get; set; }
        public AcademicTerm? Term { get; set; }  // FirstSemester or SecondSemester

        // Navigation Properties
        public StudentEnrollment Enrollment { get; set; } = null!;
        public Subject? Subject { get; set; }
    }
}
