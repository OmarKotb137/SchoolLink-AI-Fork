using Project.Domain.Entities;
using Project.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Core;

public interface IUserRepository : IRepository<User>
{
    Task<User?>              GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool>               ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task<User?>              GetActiveByIdAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<User>> GetByRoleAsync(UserRole role, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetActiveByRoleAsync(UserRole role, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetTeachersAsync(CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetParentsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetInactiveUsersAsync(CancellationToken ct = default);

    Task<IReadOnlyList<User>> SearchByNameAsync(string query, CancellationToken ct = default);
    Task<IReadOnlyList<User>> SearchByNameAndRoleAsync(string query, UserRole role, CancellationToken ct = default);

    Task<int> GetCountByRoleAsync(UserRole role, CancellationToken ct = default);
}



