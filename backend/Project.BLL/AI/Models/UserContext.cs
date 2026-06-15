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
}
