using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Communication;
using SchoolLink.Domain.Entities;
using SchoolLink.Domain.Enums;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Communication;

public class NotificationRepository : Repository<Notification>, INotificationRepository
{
    public NotificationRepository(AppDbContext context) : base(context) { }


    public async Task<IReadOnlyList<Notification>> GetByUserIdPagedAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken ct = default)
        => await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Notification>> GetUnreadByUserIdAsync(
        int userId,
        CancellationToken ct = default)
        => await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);

    public async Task<int> GetUnreadCountAsync(
        int userId,
        CancellationToken ct = default)
        => await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, ct);

    public async Task<IReadOnlyList<Notification>> GetByTypeAsync(
        int userId,
        NotificationType type,
        CancellationToken ct = default)
        => await _context.Notifications
            .Where(n => n.UserId == userId && n.Type == type)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);


    public async Task MarkAsReadAsync(
        int notificationId,
        CancellationToken ct = default)
        => await _context.Notifications
            .Where(n => n.Id == notificationId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead,    true)
                .SetProperty(n => n.ReadAt,    DateTime.UtcNow)
                .SetProperty(n => n.UpdatedAt, DateTime.UtcNow), ct);

    public async Task MarkAllAsReadAsync(
        int userId,
        CancellationToken ct = default)
        => await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead,    true)
                .SetProperty(n => n.ReadAt,    DateTime.UtcNow)
                .SetProperty(n => n.UpdatedAt, DateTime.UtcNow), ct);


    public async Task BulkAddAsync(
        IEnumerable<Notification> notifications,
        CancellationToken ct = default)
        => await _context.Notifications.AddRangeAsync(notifications, ct);
}



