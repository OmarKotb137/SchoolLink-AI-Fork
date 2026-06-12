using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.Notifications;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [Authorize(Roles = "Admin,Teacher")]
    [HttpPost]
    public async Task<IActionResult> Send([FromBody] SendNotificationRequest request)
    {
        var result = await _notificationService.SendNotificationAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetByUser), new { userId = request.UserId }, result);
    }

    [Authorize(Roles = "Admin,Teacher")]
    [HttpPost("bulk")]
    public async Task<IActionResult> SendBulk([FromBody] SendBulkNotificationRequest request)
    {
        var result = await _notificationService.SendBulkNotificationAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("bulk")]
    public async Task<IActionResult> DeleteBulk([FromBody] List<int> notificationIds)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _notificationService.DeleteBulkNotificationsAsync(notificationIds, userId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return NoContent();
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUser(int userId, [FromQuery] bool onlyUnread = false)
    {
        var result = await _notificationService.GetNotificationsByUserAsync(userId, onlyUnread);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("user/{userId}/unread-count")]
    public async Task<IActionResult> GetUnreadCount(int userId)
    {
        var result = await _notificationService.GetUnreadCountAsync(userId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("{notificationId}")]
    public async Task<IActionResult> GetById(int notificationId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _notificationService.GetNotificationByIdAsync(notificationId, userId);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpPut("{notificationId}/read")]
    public async Task<IActionResult> MarkAsRead(int notificationId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _notificationService.MarkNotificationAsReadAsync(notificationId, userId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("user/{userId}/read-all")]
    public async Task<IActionResult> MarkAllAsRead(int userId)
    {
        var result = await _notificationService.MarkAllNotificationsAsReadAsync(userId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{notificationId}")]
    public async Task<IActionResult> Delete(int notificationId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _notificationService.DeleteNotificationAsync(notificationId, userId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return NoContent();
    }
}
