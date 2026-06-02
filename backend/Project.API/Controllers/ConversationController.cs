using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.Common;
using Project.BLL.DTOs.Conversations;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ConversationController : ControllerBase
{
    private readonly IConversationService _conversationService;

    public ConversationController(IConversationService conversationService)
    {
        _conversationService = conversationService;
    }

    [HttpPost("direct")]
    public async Task<IActionResult> CreateDirect([FromBody] CreateDirectConversationRequest request)
    {
        request.InitiatorUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _conversationService.CreateDirectConversationAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetMessages), new { conversationId = result.Data?.Id }, result);
    }

    [HttpPost("group")]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupConversationRequest request)
    {
        request.CreatorUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _conversationService.CreateGroupConversationAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetMessages), new { conversationId = result.Data?.Id }, result);
    }

    [HttpPost("subject-group")]
    public async Task<IActionResult> CreateSubjectGroup([FromBody] CreateSubjectGroupConversationRequest request)
    {
        request.CreatorUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _conversationService.CreateSubjectGroupConversationAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetMessages), new { conversationId = result.Data?.Id }, result);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyConversations()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _conversationService.GetUserConversationsAsync(userId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("my/unread-count")]
    public async Task<IActionResult> GetMyUnreadCount()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _conversationService.GetUnreadMessagesCountAsync(userId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string term)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _conversationService.SearchConversationsAsync(userId, term);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("{conversationId}")]
    public async Task<IActionResult> GetById(int conversationId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _conversationService.GetConversationByIdAsync(conversationId, userId);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpPost("{conversationId}/messages")]
    public async Task<IActionResult> SendMessage(int conversationId, [FromBody] SendMessageRequest request)
    {
        request.ConversationId = conversationId;
        request.SenderId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _conversationService.SendMessageAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("{conversationId}/participants")]
    public async Task<IActionResult> GetParticipants(int conversationId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _conversationService.GetConversationParticipantsAsync(conversationId, userId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("{conversationId}/read")]
    public async Task<IActionResult> MarkAsRead(int conversationId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _conversationService.MarkConversationAsReadAsync(conversationId, userId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("{conversationId}/title")]
    public async Task<IActionResult> UpdateTitle(int conversationId, [FromBody] UpdateConversationTitleRequest request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _conversationService.UpdateConversationTitleAsync(conversationId, request.Title, userId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("{conversationId}")]
    public async Task<IActionResult> Delete(int conversationId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _conversationService.DeleteConversationAsync(conversationId, userId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return NoContent();
    }

    [HttpGet("{conversationId}/messages")]
    public async Task<IActionResult> GetMessages(int conversationId, [FromQuery] PaginationFilter filter)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _conversationService.GetMessagesAsync(conversationId, userId, filter);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("{conversationId}/participants")]
    public async Task<IActionResult> AddParticipant(int conversationId, [FromQuery] int participantUserId)
    {
        var callerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _conversationService.AddParticipantAsync(conversationId, participantUserId, callerId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("{conversationId}/participants/{participantUserId}")]
    public async Task<IActionResult> RemoveParticipant(int conversationId, int participantUserId)
    {
        var callerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _conversationService.RemoveParticipantAsync(conversationId, participantUserId, callerId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return NoContent();
    }
}
