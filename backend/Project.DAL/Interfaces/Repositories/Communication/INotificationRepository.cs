using SchoolLink.Domain.Entities;
using SchoolLink.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Communication;

public interface INotificationRepository : IRepository<Notification>
{
    Task<IReadOnlyList<Notification>> GetByUserIdPagedAsync(int userId, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<Notification>> GetUnreadByUserIdAsync(int userId, CancellationToken ct = default);
    Task<int>                         GetUnreadCountAsync(int userId, CancellationToken ct = default);
    Task<IReadOnlyList<Notification>> GetByTypeAsync(int userId, NotificationType type, CancellationToken ct = default);

    Task MarkAsReadAsync(int notificationId, CancellationToken ct = default);
    Task MarkAllAsReadAsync(int userId, CancellationToken ct = default);

    Task BulkAddAsync(IEnumerable<Notification> notifications, CancellationToken ct = default);
}



