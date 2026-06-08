namespace Project.Domain.Entities
{
    public class StudyPlan : BaseEntity
    {
        public int EnrollmentId { get; set; }
        public bool GeneratedByAI { get; set; } = false;
        public string? AIPromptSummary { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public int? RestDay { get; set; }

        // Navigation Properties
        public StudentEnrollment Enrollment { get; set; } = null!;
        public ICollection<StudyPlanItem> Items { get; set; } = new List<StudyPlanItem>();
    }
}
