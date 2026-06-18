using Microsoft.AspNetCore.SignalR;
using Project.API.Hubs;
using Project.BLL.DTOs.Notifications;
using Project.BLL.Interfaces;

namespace Project.API.Services;

public class SignalRNotificationPushService : INotificationPushService
{
    private readonly IHubContext<NotificationsHub> _hubContext;

    public SignalRNotificationPushService(IHubContext<NotificationsHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task PushToUserAsync(int userId, NotificationDto notification)
    {
        await _hubContext.Clients.Group($"user_{userId}")
            .SendAsync("ReceiveNotification", notification);
    }

    public async Task PushToUsersAsync(IEnumerable<int> userIds, NotificationDto notification)
    {
        var tasks = userIds.Select(uid =>
            _hubContext.Clients.Group($"user_{uid}")
                .SendAsync("ReceiveNotification", notification));
        await Task.WhenAll(tasks);
    }
}
