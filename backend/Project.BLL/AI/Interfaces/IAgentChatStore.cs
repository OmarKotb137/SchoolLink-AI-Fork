using Project.BLL.AI.Models;
using Project.BLL.AI.Models.History;

namespace Project.BLL.AI.Interfaces;

public interface IAgentChatStore
{
    Task SaveMessageAsync(string conversationId, int userId, string sender, string content, string agentType, CancellationToken ct = default);
    Task<List<ChatMessage>> GetRecentMessagesAsync(string conversationId, int userId, int count = 10, CancellationToken ct = default);
    Task<List<ConversationListItemDto>> GetUserConversationsAsync(int userId, string? agentType = null, CancellationToken ct = default);
    Task<List<ConversationMessageDto>> GetConversationMessagesAsync(string conversationId, int userId, CancellationToken ct = default);
    Task DeleteConversationAsync(string conversationId, int userId, CancellationToken ct = default);
}
