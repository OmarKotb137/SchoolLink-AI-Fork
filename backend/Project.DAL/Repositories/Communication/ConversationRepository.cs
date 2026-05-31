using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Communication;
using SchoolLink.Domain.Entities;
using SchoolLink.Domain.Enums;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Communication;

public class ConversationRepository : Repository<Conversation>, IConversationRepository
{
    public ConversationRepository(AppDbContext context) : base(context) { }


    public async Task<IReadOnlyList<Conversation>> GetByParticipantIdAsync(
        int userId,
        CancellationToken ct = default)
        => await _context.Conversations
            .Where(c => c.Participants.Any(p => p.UserId == userId))
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync(ct);

    public async Task<Conversation?> GetDirectConversationAsync(
        int userId1,
        int userId2,
        CancellationToken ct = default)
        => await _context.Conversations
            .Where(c =>
                c.Type == ConversationType.Direct &&
                c.Participants.Any(p => p.UserId == userId1) &&
                c.Participants.Any(p => p.UserId == userId2))
            .FirstOrDefaultAsync(ct);


    public async Task<Conversation?> GetWithParticipantsAsync(
        int conversationId,
        CancellationToken ct = default)
        => await _context.Conversations
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(c => c.Id == conversationId, ct);

    public async Task<IReadOnlyList<Conversation>> GetWithLastMessageAsync(
        int userId,
        CancellationToken ct = default)
        => await _context.Conversations
            .Where(c => c.Participants.Any(p => p.UserId == userId))
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .Include(c => c.Messages
                .OrderByDescending(m => m.SentAt)
                .Take(1))
                .ThenInclude(m => m.Sender)
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync(ct);


    public async Task<IReadOnlyList<Conversation>> GetGroupsByParticipantAsync(
        int userId,
        CancellationToken ct = default)
        => await _context.Conversations
            .Where(c =>
                c.Type == ConversationType.Group &&
                c.Participants.Any(p => p.UserId == userId))
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync(ct);


    public async Task<int> GetUnreadConversationsCountAsync(
        int userId,
        CancellationToken ct = default)
        => await _context.ConversationParticipants
            .Where(p => p.UserId == userId)
            .CountAsync(p =>
                _context.Messages.Any(m =>
                    m.ConversationId == p.ConversationId &&
                    m.SenderId       != userId           &&
                    (p.LastReadAt == null || m.SentAt > p.LastReadAt)), ct);
}



