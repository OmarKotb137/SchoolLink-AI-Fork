using Microsoft.EntityFrameworkCore;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;
using Project.DAL.Context;
using Project.Domain.Entities;

namespace Project.BLL.AI.Services;

public class AgentChatStore : IAgentChatStore
{
    private readonly AppDbContext _db;

    public AgentChatStore(AppDbContext db) => _db = db;

    public async Task SaveMessageAsync(string conversationId, string sender, string content, string agentType, CancellationToken ct = default)
    {
        _db.AgentConversationMessages.Add(new AgentConversationMessage
        {
            ConversationId = conversationId,
            Sender = sender,
            Content = content,
            AgentType = agentType,
            Timestamp = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<ChatMessage>> GetRecentMessagesAsync(string conversationId, int count = 10, CancellationToken ct = default)
    {
        var msgs = await _db.AgentConversationMessages
            .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
            .OrderByDescending(m => m.Timestamp)
            .Take(count)
            .OrderBy(m => m.Timestamp)
            .ToListAsync(ct);

        return msgs.Select(m => new ChatMessage
        {
            Role = m.Sender,
            Content = m.Content
        }).ToList();
    }
}
