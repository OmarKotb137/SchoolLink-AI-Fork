namespace Project.BLL.DTOs.Conversations;

public class CreateGroupConversationRequest
{
    public string Title { get; set; } = string.Empty;
    public int CreatorUserId { get; set; }
    public List<int> ParticipantUserIds { get; set; } = new();
}
