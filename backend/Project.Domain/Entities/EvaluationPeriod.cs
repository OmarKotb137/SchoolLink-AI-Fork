using Project.Domain.Enums;

namespace Project.Domain.Entities
{
    public class EvaluationPeriod : BaseEntity
    {
        public int AcademicYearId { get; set; }
        public string Name { get; set; } = string.Empty;
        public PeriodType PeriodType { get; set; }
        public int OrderNum { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string? MonthName { get; set; }

        // Navigation Properties
        public AcademicYear AcademicYear { get; set; } = null!;
    }
}
