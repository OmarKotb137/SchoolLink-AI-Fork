namespace Project.BLL.DTOs.Assignment;

public class AssignmentFilterDto
{
    public string? Search { get; set; }
    public int? SubjectId { get; set; }
    public string? Status { get; set; }
    public string? SortBy { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int? TeacherId { get; set; }
    public int AcademicYearId { get; set; }
}
