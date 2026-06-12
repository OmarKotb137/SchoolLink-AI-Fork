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

    private int CurrentUserId =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    /// <summary>
    /// GET /api/ai/chats?agentType=teacher
    /// جلب قائمة المحادثات السابقة للمستخدم الحالي
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetConversations([FromQuery] string? agentType, CancellationToken ct)
    {
        var userId = CurrentUserId;
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
        var messages = await _chatStore.GetConversationMessagesAsync(conversationId, ct);

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
        var userId = CurrentUserId;
        await _chatStore.DeleteConversationAsync(conversationId, userId, ct);
        return Ok(OperationResult.Success("تم حذف المحادثة"));
    }
}
