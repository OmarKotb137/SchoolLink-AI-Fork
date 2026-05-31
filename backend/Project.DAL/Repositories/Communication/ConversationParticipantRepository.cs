using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Communication;
using SchoolLink.Domain.Entities;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Communication;

public class ConversationParticipantRepository
    : Repository<ConversationParticipant>, IConversationParticipantRepository
{
    public ConversationParticipantRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<ConversationParticipant>> GetByConversationIdAsync(
        int conversationId,
        CancellationToken ct = default)
        => await _context.ConversationParticipants
            .Where(p => p.ConversationId == conversationId)
            .Include(p => p.User)
            .OrderBy(p => p.User.FullName)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ConversationParticipant>> GetByUserIdAsync(
        int userId,
        CancellationToken ct = default)
        => await _context.ConversationParticipants
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.JoinedAt)
            .ToListAsync(ct);

    public async Task<ConversationParticipant?> GetByConversationAndUserAsync(
        int conversationId,
        int userId,
        CancellationToken ct = default)
        => await _context.ConversationParticipants
            .FirstOrDefaultAsync(p =>
                p.ConversationId == conversationId &&
                p.UserId         == userId, ct);

    public async Task<bool> IsParticipantAsync(
        int conversationId,
        int userId,
        CancellationToken ct = default)
        => await _context.ConversationParticipants
            .AnyAsync(p =>
                p.ConversationId == conversationId &&
                p.UserId         == userId, ct);

    public async Task UpdateLastReadAtAsync(
        int conversationId,
        int userId,
        DateTime readAt,
        CancellationToken ct = default)
        => await _context.ConversationParticipants
            .Where(p =>
                p.ConversationId == conversationId &&
                p.UserId         == userId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.LastReadAt, readAt)
                .SetProperty(p => p.UpdatedAt,  DateTime.UtcNow), ct);
}



