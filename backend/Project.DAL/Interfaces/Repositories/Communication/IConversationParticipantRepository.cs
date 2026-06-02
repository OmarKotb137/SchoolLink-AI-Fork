using Project.Domain.Entities;
using Project.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Communication;

public interface IConversationParticipantRepository : IRepository<ConversationParticipant>
{
    Task<IReadOnlyList<ConversationParticipant>> GetByConversationIdAsync(int conversationId, CancellationToken ct = default);
    Task<IReadOnlyList<ConversationParticipant>> GetByUserIdAsync(int userId, CancellationToken ct = default);
    Task<ConversationParticipant?>               GetByConversationAndUserAsync(int conversationId, int userId, CancellationToken ct = default);
    Task<bool>                                   IsParticipantAsync(int conversationId, int userId, CancellationToken ct = default);
    Task                                         UpdateLastReadAtAsync(int conversationId, int userId, DateTime readAt, CancellationToken ct = default);
}



