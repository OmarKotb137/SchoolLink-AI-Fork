namespace Project.Domain.Entities;

public class AgentConversationMessage : BaseEntity
{
    public string ConversationId { get; set; } = string.Empty;
    public string Sender { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string AgentType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
