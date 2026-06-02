using SchoolLink.Domain.Enums;

namespace Project.BLL.DTOs.Library;

public class LibraryItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public LibraryItemType ItemType { get; set; }
    public string? FileUrl { get; set; }
    public int? SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public int? GradeLevelId { get; set; }
    public string? GradeLevelName { get; set; }
    public int? AcademicYearId { get; set; }
    public int UploadedById { get; set; }
    public string UploadedByName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public long? FileSizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
}
