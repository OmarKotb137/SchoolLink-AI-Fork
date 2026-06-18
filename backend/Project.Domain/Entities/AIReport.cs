using Project.Domain.Enums;

namespace Project.Domain.Entities;

public class AIReport : BaseEntity
{
    public int StudentId { get; set; }
    public int? PeriodId { get; set; }
    public int? ClassId { get; set; }
    public AcademicTerm? Term { get; set; }
    public string ReportType { get; set; } = string.Empty; // "Student", "Class", "Recommendations"
    public string Content { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public bool IsPublished { get; set; }

    // Navigation Properties
    public Student Student { get; set; } = null!;
    public EvaluationPeriod? Period { get; set; }
    public SchoolClass? Class { get; set; }
}
