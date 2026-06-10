using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs.Conversations;

public class UpdateMessageRequest
{
    [Required(ErrorMessage = "Message content is required")]
    [StringLength(5000, MinimumLength = 1, ErrorMessage = "Message must be between 1 and 5000 characters")]
    public string Content { get; set; } = string.Empty;
}
