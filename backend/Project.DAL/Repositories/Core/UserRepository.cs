using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Core;
using Project.Domain.Entities;
using Project.Domain.Enums;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Core;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }


    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email.Trim().ToLower(), ct);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
        => await _context.Users
            .AnyAsync(u => u.Email == email.Trim().ToLower(), ct);

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



