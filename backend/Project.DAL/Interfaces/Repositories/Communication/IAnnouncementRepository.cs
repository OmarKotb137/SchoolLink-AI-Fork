using SchoolLink.Domain.Entities;
using SchoolLink.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Communication;

public interface IAnnouncementRepository : IRepository<Announcement>
{
    Task<IReadOnlyList<Announcement>> GetActiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Announcement>> GetByTargetRoleAsync(UserRole? role, CancellationToken ct = default);
    Task<IReadOnlyList<Announcement>> GetByClassIdAsync(int classId, CancellationToken ct = default);
    Task<IReadOnlyList<Announcement>> GetForUserAsync(int classId, UserRole role, CancellationToken ct = default);
    Task<IReadOnlyList<Announcement>> GetByAuthorIdAsync(int userId, CancellationToken ct = default);
    Task<IReadOnlyList<Announcement>> GetExpiredAsync(CancellationToken ct = default);
}



