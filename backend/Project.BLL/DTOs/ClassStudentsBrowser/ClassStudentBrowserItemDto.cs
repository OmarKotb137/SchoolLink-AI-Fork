using Project.Domain.Enums;

namespace Project.BLL.DTOs.ClassStudentsBrowser;

public class ClassStudentBrowserItemDto
{
    public int EnrollmentId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public Gender? Gender { get; set; }
    public bool IsActive { get; set; }
    public DateOnly EnrolledAt { get; set; }
}
