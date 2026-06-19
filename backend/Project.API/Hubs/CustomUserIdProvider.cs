using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace Project.API.Hubs;

public class CustomUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
