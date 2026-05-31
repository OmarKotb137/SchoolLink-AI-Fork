using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Communication;
using SchoolLink.Domain.Entities;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Communication;

public class MessageRepository : Repository<Message>, IMessageRepository
{
    public MessageRepository(AppDbContext context) : base(context) { }


    public async Task<IReadOnlyList<Message>> GetByConversationPagedAsync(
        int conversationId,
        int page,
        int pageSize,
        CancellationToken ct = default)
        => await _context.Messages
            .Where(m => m.ConversationId == conversationId)
            .Include(m => m.Sender)
            .OrderByDescending(m => m.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Message>> GetAfterMessageIdAsync(
        int conversationId,
        int lastMessageId,
        CancellationToken ct = default)
        => await _context.Messages
            .Where(m =>
                m.ConversationId == conversationId &&
                m.Id             >  lastMessageId)
            .Include(m => m.Sender)
            .OrderBy(m => m.SentAt)
            .ToListAsync(ct);


    public async Task<Message?> GetLatestByConversationAsync(
        int conversationId,
        CancellationToken ct = default)
        => await _context.Messages
            .Where(m => m.ConversationId == conversationId)
            .Include(m => m.Sender)
            .OrderByDescending(m => m.SentAt)
            .FirstOrDefaultAsync(ct);


    public async Task<int> GetUnreadCountAsync(
        int conversationId,
        int userId,
        CancellationToken ct = default)
    {
        var lastReadAt = await _context.ConversationParticipants
            .Where(p => p.ConversationId == conversationId && p.UserId == userId)
            .Select(p => p.LastReadAt)
            .FirstOrDefaultAsync(ct);

        return await _context.Messages
            .CountAsync(m =>
                m.ConversationId == conversationId    &&
                m.SenderId       != userId             &&
                (lastReadAt == null || m.SentAt > lastReadAt), ct);
    }


    public async Task<IReadOnlyList<Message>> GetByDateRangeAsync(
        int conversationId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default)
        => await _context.Messages
            .Where(m =>
                m.ConversationId == conversationId &&
                m.SentAt         >= from           &&
                m.SentAt         <= to)
            .Include(m => m.Sender)
            .OrderBy(m => m.SentAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Message>> GetWithAttachmentsAsync(
        int conversationId,
        CancellationToken ct = default)
        => await _context.Messages
            .Where(m =>
                m.ConversationId == conversationId &&
                m.AttachmentUrl  != null)
            .Include(m => m.Sender)
            .OrderByDescending(m => m.SentAt)
            .ToListAsync(ct);
}



