using Project.Domain.Enums;

namespace Project.BLL.DTOs.Library;

public class CreateLibraryItemRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public LibraryItemType ItemType { get; set; }
    public string? FileUrl { get; set; }
    public int? SubjectId { get; set; }
    public int? GradeLevelId { get; set; }
    public int? AcademicYearId { get; set; }
    public int UploadedById { get; set; }
}
