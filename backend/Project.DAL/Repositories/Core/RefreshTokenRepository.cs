using Microsoft.EntityFrameworkCore;
using Project.DAL.Context;
using Project.DAL.Interfaces.Repositories.Core;
using Project.Domain.Entities;

namespace Project.DAL.Repositories.Core;

public class RefreshTokenRepository : Repository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(AppDbContext context) : base(context) { }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(rt => rt.Token == token && !rt.IsRevoked, ct);

    public async Task RevokeAllForUserAsync(int userId, CancellationToken ct = default)
        => await _dbSet
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(rt => rt.IsRevoked, true)
                .SetProperty(rt => rt.RevokedAt, DateTime.UtcNow),
            ct);
}
