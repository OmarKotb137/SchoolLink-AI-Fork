using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs.Conversations;

public class UpdateConversationTitleRequest
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 200 characters")]
    public string Title { get; set; } = string.Empty;
}
