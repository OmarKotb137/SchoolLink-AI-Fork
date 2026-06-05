namespace Project.BLL.DTOs.Students;

public class StudentSearchFilter
{
    public string SearchTerm { get; set; } = string.Empty;
    public int? ClassId { get; set; }
    public int? AcademicYearId { get; set; }
    public bool? IsActive { get; set; }
}
