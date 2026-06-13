using System.Security.Claims;
using Common.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models.History;

namespace Project.API.Controllers.AI;

[Route("api/ai/chats")]
[ApiController]
[Authorize]
public class ChatHistoryController : ControllerBase
{
    private readonly IAgentChatStore _chatStore;

    public ChatHistoryController(IAgentChatStore chatStore) => _chatStore = chatStore;

    private int? CurrentUserId
    {
        get
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim is null || !int.TryParse(claim.Value, out var userId))
                return null;
            return userId;
        }
    }

    private IActionResult? UnauthorizedIfMissingUserId(out int userId)
    {
        var id = CurrentUserId;
        if (!id.HasValue)
        {
            userId = 0;
            return Unauthorized(OperationResult.Failure("المستخدم غير موجود", 401));
        }
        userId = id.Value;
        return null;
    }

    /// <summary>
    /// GET /api/ai/chats?agentType=teacher
    /// جلب قائمة المحادثات السابقة للمستخدم الحالي
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetConversations([FromQuery] string? agentType, CancellationToken ct)
    {
        var unauthorized = UnauthorizedIfMissingUserId(out var userId);
        if (unauthorized is not null) return unauthorized;

        var conversations = await _chatStore.GetUserConversationsAsync(userId, agentType, ct);

        return Ok(OperationResult<List<ConversationListItemDto>>.Success(conversations));
    }

    /// <summary>
    /// GET /api/ai/chats/{conversationId}/messages
    /// جلب رسائل محادثة معينة
    /// </summary>
    [HttpGet("{conversationId}/messages")]
    public async Task<IActionResult> GetMessages(string conversationId, CancellationToken ct)
    {
        var unauthorized = UnauthorizedIfMissingUserId(out var userId);
        if (unauthorized is not null) return unauthorized;

        var messages = await _chatStore.GetConversationMessagesAsync(conversationId, userId, ct);

        if (messages.Count == 0)
            return NotFound(OperationResult.Failure("المحادثة غير موجودة", 404));

        return Ok(OperationResult<List<ConversationMessageDto>>.Success(messages));
    }

    /// <summary>
    /// DELETE /api/ai/chats/{conversationId}
    /// حذف محادثة كاملة (soft delete)
    /// </summary>
    [HttpDelete("{conversationId}")]
    public async Task<IActionResult> DeleteConversation(string conversationId, CancellationToken ct)
    {
        var unauthorized = UnauthorizedIfMissingUserId(out var userId);
        if (unauthorized is not null) return unauthorized;

        await _chatStore.DeleteConversationAsync(conversationId, userId, ct);
        return Ok(OperationResult.Success("تم حذف المحادثة"));
    }
}
