using Project.Domain.Enums;

namespace Project.Domain.Entities
{
    public class StudyPlanItem : BaseEntity
    {
        public int StudyPlanId { get; set; }
        public int SubjectId { get; set; }
        public int DayOfWeek { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public string? Topic { get; set; }
        public string? Notes { get; set; }
        public bool IsCompleted { get; set; } = false;

        // Navigation Properties
        public StudyPlan StudyPlan { get; set; } = null!;
        public Subject Subject { get; set; } = null!;
    }
}
