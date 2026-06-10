using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Project.BLL.Interfaces;

namespace Project.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IConversationService _conversationService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(IConversationService conversationService, ILogger<ChatHub> logger)
    {
        _conversationService = conversationService;
        _logger = logger;
    }

    private int GetUserId() =>
        int.Parse(Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    public async Task JoinConversation(int conversationId)
    {
        var userId = GetUserId();
        var result = await _conversationService.GetConversationByIdAsync(conversationId, userId);
        if (!result.IsSuccess)
        {
            await Clients.Caller.SendAsync("Error", "لا يمكن الانضمام إلى هذه المحادثة");
            return;
        }
        await Groups.AddToGroupAsync(Context.ConnectionId, $"conv_{conversationId}");
        _logger.LogInformation("User {UserId} joined conversation {ConvId}", userId, conversationId);
    }

    public async Task LeaveConversation(int conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conv_{conversationId}");
    }

    public async Task MarkAsRead(int conversationId)
    {
        var userId = GetUserId();
        await _conversationService.MarkConversationAsReadAsync(conversationId, userId);
        await Clients.Group($"conv_{conversationId}").SendAsync("ReadReceipt", conversationId, userId);
    }

    public async Task UserTyping(int conversationId)
    {
        var userId = GetUserId();
        await Clients.OthersInGroup($"conv_{conversationId}").SendAsync("UserTyping", conversationId, userId);
    }

    public async Task UserStoppedTyping(int conversationId)
    {
        var userId = GetUserId();
        await Clients.OthersInGroup($"conv_{conversationId}").SendAsync("UserStoppedTyping", conversationId, userId);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("ChatHub connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("ChatHub disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
