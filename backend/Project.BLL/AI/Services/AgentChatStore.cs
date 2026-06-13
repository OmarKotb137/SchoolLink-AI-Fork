using Microsoft.EntityFrameworkCore;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;
using Project.BLL.AI.Models.History;
using Project.DAL.Context;
using Project.Domain.Entities;

namespace Project.BLL.AI.Services;

public class AgentChatStore : IAgentChatStore
{
    private readonly AppDbContext _db;

    public AgentChatStore(AppDbContext db) => _db = db;

    public async Task SaveMessageAsync(string conversationId, int userId, string sender, string content, string agentType, CancellationToken ct = default)
    {
        _db.AgentConversationMessages.Add(new AgentConversationMessage
        {
            ConversationId = conversationId,
            UserId = userId,
            Sender = sender,
            Content = content,
            AgentType = agentType,
            Timestamp = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<ChatMessage>> GetRecentMessagesAsync(string conversationId, int userId, int count = 10, CancellationToken ct = default)
    {
        var msgs = await _db.AgentConversationMessages
            .Where(m => m.ConversationId == conversationId && m.UserId == userId && !m.IsDeleted)
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

    public async Task<List<ConversationListItemDto>> GetUserConversationsAsync(int userId, string? agentType = null, CancellationToken ct = default)
    {
        var query = _db.AgentConversationMessages
            .Where(m => m.UserId == userId && !m.IsDeleted);

        if (!string.IsNullOrWhiteSpace(agentType))
            query = query.Where(m => m.AgentType == agentType);

        var grouped = await query
            .GroupBy(m => m.ConversationId)
            .Select(g => new
            {
                ConversationId = g.Key,
                AgentType = g.First().AgentType,
                LastMessageAt = g.Max(m => m.Timestamp),
                MessageCount = g.Count(),
                FirstUserMessage = g
                    .Where(m => m.Sender == "user")
                    .OrderBy(m => m.Timestamp)
                    .Select(m => m.Content)
                    .FirstOrDefault()
            })
            .OrderByDescending(x => x.LastMessageAt)
            .ToListAsync(ct);

        return grouped.Select(g => new ConversationListItemDto
        {
            ConversationId = g.ConversationId,
            AgentType = g.AgentType,
            Summary = g.FirstUserMessage?.Length > 100
                ? g.FirstUserMessage[..100] + "…"
                : g.FirstUserMessage ?? "(بداية محادثة)",
            LastMessageAt = g.LastMessageAt,
            MessageCount = g.MessageCount
        }).ToList();
    }

    public async Task<List<ConversationMessageDto>> GetConversationMessagesAsync(string conversationId, int userId, CancellationToken ct = default)
    {
        var msgs = await _db.AgentConversationMessages
            .Where(m => m.ConversationId == conversationId && m.UserId == userId && !m.IsDeleted)
            .OrderBy(m => m.Timestamp)
            .ToListAsync(ct);

        return msgs.Select(m => new ConversationMessageDto
        {
            Sender = m.Sender,
            Content = m.Content,
            Timestamp = m.Timestamp
        }).ToList();
    }

    public async Task DeleteConversationAsync(string conversationId, int userId, CancellationToken ct = default)
    {
        var messages = await _db.AgentConversationMessages
            .Where(m => m.ConversationId == conversationId && m.UserId == userId && !m.IsDeleted)
            .ToListAsync(ct);

        foreach (var msg in messages)
        {
            msg.IsDeleted = true;
            msg.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }
}
