namespace Project.BLL.AI.Models;

public class AgentChatRequest
{
    public string Message { get; set; } = string.Empty;
    public string? ConversationId { get; set; }
    public UserContext? Context { get; set; }
}
