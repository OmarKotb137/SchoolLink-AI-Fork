using Project.BLL.AI.Models;

namespace Project.BLL.AI.Interfaces;

public interface IAgentChatStore
{
    Task SaveMessageAsync(string conversationId, string sender, string content, string agentType, CancellationToken ct = default);
    Task<List<ChatMessage>> GetRecentMessagesAsync(string conversationId, int count = 10, CancellationToken ct = default);
}
