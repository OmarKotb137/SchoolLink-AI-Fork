using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Communication;
using SchoolLink.Domain.Entities;
using SchoolLink.Domain.Enums;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Communication;

public class AnnouncementRepository : Repository<Announcement>, IAnnouncementRepository
{
    public AnnouncementRepository(AppDbContext context) : base(context) { }


    public async Task<IReadOnlyList<Announcement>> GetActiveAsync(
        CancellationToken ct = default)
        => await _context.Announcements
            .Where(a => a.ExpiresAt == null || a.ExpiresAt > DateTime.UtcNow)
            .Include(a => a.Author)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);


    public async Task<IReadOnlyList<Announcement>> GetByTargetRoleAsync(
        UserRole? role,
        CancellationToken ct = default)
        => await _context.Announcements
            .Where(a =>
                a.TargetRole == role &&
                (a.ExpiresAt == null || a.ExpiresAt > DateTime.UtcNow))
            .Include(a => a.Author)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Announcement>> GetByClassIdAsync(
        int classId,
        CancellationToken ct = default)
        => await _context.Announcements
            .Where(a =>
                (a.TargetClassId == null || a.TargetClassId == classId) &&
                (a.ExpiresAt     == null || a.ExpiresAt     > DateTime.UtcNow))
            .Include(a => a.Author)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Announcement>> GetForUserAsync(
        int classId,
        UserRole role,
        CancellationToken ct = default)
        => await _context.Announcements
            .Where(a =>
                (a.ExpiresAt     == null || a.ExpiresAt     > DateTime.UtcNow) &&
                (a.TargetRole    == null || a.TargetRole    == role)            &&
                (a.TargetClassId == null || a.TargetClassId == classId))
            .Include(a => a.Author)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);


    public async Task<IReadOnlyList<Announcement>> GetByAuthorIdAsync(
        int userId,
        CancellationToken ct = default)
        => await _context.Announcements
            .Where(a => a.AuthorId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);


    public async Task<IReadOnlyList<Announcement>> GetExpiredAsync(
        CancellationToken ct = default)
        => await _context.Announcements
            .Where(a => a.ExpiresAt != null && a.ExpiresAt < DateTime.UtcNow)
            .OrderByDescending(a => a.ExpiresAt)
            .ToListAsync(ct);
}



