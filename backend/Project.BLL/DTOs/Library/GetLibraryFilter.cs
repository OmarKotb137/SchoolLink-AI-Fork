using Project.BLL.DTOs.Common;
using SchoolLink.Domain.Enums;

namespace Project.BLL.DTOs.Library;

public class GetLibraryFilter : PaginationFilter
{
    public int? SubjectId { get; set; }
    public int? GradeLevelId { get; set; }
    public int? AcademicYearId { get; set; }
    public LibraryItemType? ItemType { get; set; }
    public string? SearchTerm { get; set; }
}
