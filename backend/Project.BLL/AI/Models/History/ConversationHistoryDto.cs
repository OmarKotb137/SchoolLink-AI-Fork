using Project.Domain.Entities;

namespace Project.BLL.AI.Models.History;

public class ConversationListItemDto
{
    public string ConversationId { get; set; } = string.Empty;
    public string AgentType { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public DateTime LastMessageAt { get; set; }
    public int MessageCount { get; set; }
}

public class ConversationMessageDto
{
    public string Sender { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
