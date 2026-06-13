using Project.Domain.Entities;
using Project.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Core;

public interface IUserRepository : IRepository<User>
{
    // ✅ كان GetByEmailAsync/ExistsByEmailAsync — أصبح Username
    Task<User?>  GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<bool>   ExistsByUsernameAsync(string username, CancellationToken ct = default);

    // ✅ جديد — للـ ContactEmail (للـ OTP لاحقاً)
    Task<User?>  GetByContactEmailAsync(string email, CancellationToken ct = default);

    // ✅ جديد — للـ Parent dedup بالتليفون (service-level check)
    Task<User?>  GetParentByPhoneAsync(string phone, CancellationToken ct = default);

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
