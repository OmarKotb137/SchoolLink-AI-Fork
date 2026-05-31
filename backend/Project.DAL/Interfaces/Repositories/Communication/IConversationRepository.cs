using SchoolLink.Domain.Entities;
using SchoolLink.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Communication;

public interface IConversationRepository : IRepository<Conversation>
{
    Task<IReadOnlyList<Conversation>> GetByParticipantIdAsync(int userId, CancellationToken ct = default);
    Task<Conversation?>               GetDirectConversationAsync(int userId1, int userId2, CancellationToken ct = default);
    Task<Conversation?>               GetWithParticipantsAsync(int conversationId, CancellationToken ct = default);
    Task<IReadOnlyList<Conversation>> GetWithLastMessageAsync(int userId, CancellationToken ct = default);
    Task<IReadOnlyList<Conversation>> GetGroupsByParticipantAsync(int userId, CancellationToken ct = default);
    Task<int>                         GetUnreadConversationsCountAsync(int userId, CancellationToken ct = default);
}



