using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs.Conversations;

public class CreateDirectConversationRequest
{
    public int InitiatorUserId { get; set; }

    [Required(ErrorMessage = "Target user ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid target user ID")]
    public int TargetUserId { get; set; }
}
