using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Project.API.Hubs;

[Authorize]
public class NotificationsHub : Hub
{
    private readonly ILogger<NotificationsHub> _logger;

    public NotificationsHub(ILogger<NotificationsHub> logger)
    {
        _logger = logger;
    }

    /// <summary>قراءة userId من التوكن مباشرة (نفس أسلوب ChatHub) لتجنب أي التباس في IUserIdProvider</summary>
    private int GetUserId()
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null)
            throw new HubException("User identifier claim not found in the token");
        return int.Parse(claim.Value);
    }

    public override async Task OnConnectedAsync()
    {
        try
        {
            var userId = GetUserId();
            _logger.LogInformation("NotificationsHub connected: User {UserId}, Connection {ConnectionId}",
                userId, Context.ConnectionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register connection {ConnectionId} in NotificationsHub",
                Context.ConnectionId);
            throw; // يمنع الاتصال من الاستمرار بدون تسجيل صحيح
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("NotificationsHub disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
