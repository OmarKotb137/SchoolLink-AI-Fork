namespace Project.BLL.DTOs.Conversations;

public class CreateDirectConversationRequest
{
    public int InitiatorUserId { get; set; }
    public int TargetUserId { get; set; }
}
