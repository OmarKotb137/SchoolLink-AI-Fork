namespace SchoolLink.Domain.Entities
{
    public class StudyPlan : BaseEntity
    {
        public int EnrollmentId { get; set; }
        public bool GeneratedByAI { get; set; } = false;
        public string? AIPromptSummary { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public StudentEnrollment Enrollment { get; set; } = null!;
        public ICollection<StudyPlanItem> Items { get; set; } = new List<StudyPlanItem>();
    }
}