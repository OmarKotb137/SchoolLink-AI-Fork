using Project.Domain.Entities;
using Project.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Communication;

public interface IMessageRepository : IRepository<Message>
{
    Task<IReadOnlyList<Message>> GetByConversationPagedAsync(int conversationId, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<Message>> GetAfterMessageIdAsync(int conversationId, int lastMessageId, CancellationToken ct = default);
    Task<Message?>               GetLatestByConversationAsync(int conversationId, CancellationToken ct = default);
    Task<int>                    GetUnreadCountAsync(int conversationId, int userId, CancellationToken ct = default);
    Task<IReadOnlyList<Message>> GetByDateRangeAsync(int conversationId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<Message>> GetWithAttachmentsAsync(int conversationId, CancellationToken ct = default);
}



