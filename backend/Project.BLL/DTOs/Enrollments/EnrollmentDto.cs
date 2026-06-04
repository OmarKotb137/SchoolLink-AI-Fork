namespace Project.BLL.DTOs.Enrollments;

public class EnrollmentDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public int AcademicYearId { get; set; }
    public string AcademicYearName { get; set; } = string.Empty;
    public DateOnly EnrolledAt { get; set; }
    public DateOnly? LeftAt { get; set; }
    public string? TransferReason { get; set; }
    public bool IsActive => LeftAt == null;
}
