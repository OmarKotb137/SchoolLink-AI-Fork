using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs.Conversations;

public class CreateGroupConversationRequest
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 200 characters")]
    public string Title { get; set; } = string.Empty;

    public int CreatorUserId { get; set; }

    [Required(ErrorMessage = "At least one participant is required")]
    [MinLength(1, ErrorMessage = "At least one participant is required")]
    public List<int> ParticipantUserIds { get; set; } = new();
}
