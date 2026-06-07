using Project.BLL.DTOs.Common;

namespace Project.BLL.DTOs.ClassStudentsBrowser;

public class GetClassStudentsBrowserFilter : PaginationFilter
{
    public int AcademicYearId { get; set; }
    public string? SearchTerm { get; set; }
}
