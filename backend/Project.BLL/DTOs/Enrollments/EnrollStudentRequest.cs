namespace Project.BLL.DTOs.Enrollments;

public class EnrollStudentRequest
{
    public int StudentId { get; set; }
    public int ClassId { get; set; }
    public int AcademicYearId { get; set; }
    public DateOnly EnrolledAt { get; set; }
}
