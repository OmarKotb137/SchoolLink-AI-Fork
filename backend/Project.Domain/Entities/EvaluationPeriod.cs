using SchoolLink.Domain.Enums;

namespace SchoolLink.Domain.Entities
{
    public class EvaluationPeriod : BaseEntity
    {
        public int AcademicYearId { get; set; }
        public string Name { get; set; } = string.Empty;
        public PeriodType PeriodType { get; set; }
        public int OrderNum { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? MonthName { get; set; }

        // Navigation Properties
        public AcademicYear AcademicYear { get; set; } = null!;
    }
}