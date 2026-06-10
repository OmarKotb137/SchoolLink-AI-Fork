using Project.Domain.Entities;

namespace Project.DAL.Interfaces.Repositories.Communication;

public interface IBlockedUserRepository : IRepository<BlockedUser>
{
    Task<bool> IsBlockedAsync(int blockerId, int blockedUserId, int conversationId, CancellationToken ct = default);
    Task<BlockedUser?> GetBlockAsync(int blockerId, int blockedUserId, int conversationId, CancellationToken ct = default);
}
