using Project.Domain.Enums;

namespace Project.BLL.AI.Models;

public class UserContext
{
    public int UserId { get; set; }
    public string UserRole { get; set; } = string.Empty;

    public int? EnrollmentId { get; set; }
    public int? StudentId { get; set; }
    public int? ClassId { get; set; }
    public int? GradeLevelId { get; set; }
    public int? AcademicYearId { get; set; }

    public int? TeacherId { get; set; }

    public int? ParentId { get; set; }

    /// <summary>Current academic term (null = whole year, 1 = FirstSemester, 2 = SecondSemester).</summary>
    public AcademicTerm? CurrentTerm { get; set; }
}
