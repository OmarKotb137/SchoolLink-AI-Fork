using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Core;
using Project.Domain.Entities;
using Project.Domain.Enums;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Core;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    // IgnoreQueryFilters() is intentional here:
    // The global query filter hides soft-deleted records, but username uniqueness must
    // be checked against ALL rows in the DB (active + deleted) because the unique
    // index IX_Users_Username still covers them. Without IgnoreQueryFilters, the check
    // passes for a deleted user's username and the DB throws a duplicate-key exception.
    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
        => await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Username == username.Trim().ToLower(), ct);

    public async Task<bool> ExistsByUsernameAsync(string username, CancellationToken ct = default)
        => await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Username == username.Trim().ToLower(), ct);

    public async Task<User?> GetByContactEmailAsync(string email, CancellationToken ct = default)
        => await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.ContactEmail == email.Trim().ToLower(), ct);

    // ✅ جديد — للـ Parent dedup بالتليفون (active فقط، بدون IgnoreQueryFilters)
    public async Task<User?> GetParentByPhoneAsync(string phone, CancellationToken ct = default)
        => await _context.Users
            .FirstOrDefaultAsync(u => u.Phone == phone && u.Role == UserRole.Parent && !u.IsDeleted, ct);

    public async Task<User?> GetActiveByIdAsync(int id, CancellationToken ct = default)
        => await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.IsActive, ct);


    public async Task<IReadOnlyList<User>> GetByRoleAsync(
        UserRole role,
        CancellationToken ct = default)
        => await _context.Users
            .Where(u => u.Role == role)
            .OrderBy(u => u.FullName)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<User>> GetActiveByRoleAsync(
        UserRole role,
        CancellationToken ct = default)
        => await _context.Users
            .Where(u => u.Role == role && u.IsActive)
            .OrderBy(u => u.FullName)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<User>> GetTeachersAsync(CancellationToken ct = default)
        => await _context.Users
            .Where(u => u.Role == UserRole.Teacher && u.IsActive)
            .OrderBy(u => u.FullName)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<User>> GetParentsAsync(CancellationToken ct = default)
        => await _context.Users
            .Where(u => u.Role == UserRole.Parent && u.IsActive)
            .OrderBy(u => u.FullName)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<User>> GetInactiveUsersAsync(CancellationToken ct = default)
        => await _context.Users
            .Where(u => !u.IsActive)
            .OrderBy(u => u.Role)
            .ThenBy(u => u.FullName)
            .ToListAsync(ct);


    public async Task<IReadOnlyList<User>> SearchByNameAsync(
        string query,
        CancellationToken ct = default)
        => await _context.Users
            .Where(u => u.FullName.Contains(query))
            .OrderBy(u => u.FullName)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<User>> SearchByNameAndRoleAsync(
        string query,
        UserRole role,
        CancellationToken ct = default)
        => await _context.Users
            .Where(u => u.FullName.Contains(query) && u.Role == role)
            .OrderBy(u => u.FullName)
            .ToListAsync(ct);


    public async Task<int> GetCountByRoleAsync(
        UserRole role,
        CancellationToken ct = default)
        => await _context.Users
            .CountAsync(u => u.Role == role, ct);
}
