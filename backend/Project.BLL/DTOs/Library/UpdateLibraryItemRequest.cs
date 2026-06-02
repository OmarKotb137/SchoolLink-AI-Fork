using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs.Library;

public class UpdateLibraryItemRequest
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 200 characters")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
    public int CallerUserId { get; set; }
}
