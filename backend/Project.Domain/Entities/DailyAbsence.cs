namespace Project.Domain.Entities
{
    public class DailyAbsence : BaseEntity
    {
        public int EnrollmentId { get; set; }
        public int? ClassSubjectTeacherId { get; set; }
        public DateOnly AbsenceDate { get; set; }
        public int? PeriodId { get; set; }
        public bool IsAbsent { get; set; } = true;
        public string? Reason { get; set; }
        public int? RecordedById { get; set; }

        // Navigation Properties
        public StudentEnrollment Enrollment { get; set; } = null!;
        public ClassSubjectTeacher? ClassSubjectTeacher { get; set; }
        public EvaluationPeriod? Period { get; set; }
        public User? RecordedBy { get; set; }
    }
}
