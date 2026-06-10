using Microsoft.EntityFrameworkCore;
using Project.DAL.Context;
using Project.DAL.Interfaces.Repositories.Communication;
using Project.Domain.Entities;

namespace Project.DAL.Repositories.Communication;

public class BlockedUserRepository : Repository<BlockedUser>, IBlockedUserRepository
{
    public BlockedUserRepository(AppDbContext context) : base(context) { }

    public async Task<bool> IsBlockedAsync(int blockerId, int blockedUserId, int conversationId, CancellationToken ct = default)
        => await _dbSet.AnyAsync(b => b.BlockerId == blockerId && b.BlockedUserId == blockedUserId && b.ConversationId == conversationId, ct);

    public async Task<BlockedUser?> GetBlockAsync(int blockerId, int blockedUserId, int conversationId, CancellationToken ct = default)
        => await _dbSet.IgnoreQueryFilters().FirstOrDefaultAsync(b => b.BlockerId == blockerId && b.BlockedUserId == blockedUserId && b.ConversationId == conversationId, ct);
}
