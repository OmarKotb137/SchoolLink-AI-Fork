namespace Project.BLL.DTOs.Enrollments;

public class BulkEnrollStudentsRequest
{
    public List<int> StudentIds { get; set; } = new();
    public int ClassId { get; set; }
    public int AcademicYearId { get; set; }
    public DateOnly EnrolledAt { get; set; }
}
