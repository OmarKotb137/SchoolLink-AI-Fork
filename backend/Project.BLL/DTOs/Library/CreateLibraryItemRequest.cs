using System.ComponentModel.DataAnnotations;
using SchoolLink.Domain.Enums;

namespace Project.BLL.DTOs.Library;

public class CreateLibraryItemRequest
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 200 characters")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Item type is required")]
    public LibraryItemType ItemType { get; set; }

    public string? FileUrl { get; set; }
    public int? SubjectId { get; set; }
    public int? GradeLevelId { get; set; }
    public int? AcademicYearId { get; set; }

    public int UploadedById { get; set; }
}
