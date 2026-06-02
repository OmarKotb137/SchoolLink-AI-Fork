using Project.Domain.Entities;

namespace Project.DAL.Interfaces.Repositories.Core;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task RevokeAllForUserAsync(int userId, CancellationToken ct = default);
}
