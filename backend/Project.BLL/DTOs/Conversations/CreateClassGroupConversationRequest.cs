using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs.Conversations;

public class CreateClassGroupConversationRequest
{
    public int CreatorUserId { get; set; }

    [Required(ErrorMessage = "Class ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid class ID")]
    public int ClassId { get; set; }

    [Required(ErrorMessage = "Academic year ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid academic year ID")]
    public int AcademicYearId { get; set; }

    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string? Title { get; set; }
}
