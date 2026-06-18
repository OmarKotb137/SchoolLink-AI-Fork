using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.Meetings;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[Authorize]
[ApiController]
[Route("api/parent-meeting")]
public class ParentMeetingController : ControllerBase
{
    private readonly IParentMeetingService _parentMeetingService;

    public ParentMeetingController(IParentMeetingService parentMeetingService)
    {
        _parentMeetingService = parentMeetingService;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    /// <summary>
    /// إنشاء طلب اجتماع (ولي الأمر هو اللي بيقدم)
    /// </summary>
    [Authorize(Roles = "Parent")]
    [HttpPost]
    public async Task<IActionResult> CreateRequest([FromBody] CreateMeetingRequest request)
    {
        // نستخدم userId من التوكن مش من الـ request عشان الأمان
        request.ParentId = CurrentUserId;
        var result = await _parentMeetingService.CreateRequestAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result);
    }

    /// <summary>
    /// جلب طلبات ولي أمر معين
    /// </summary>
    [HttpGet("parent/{parentId}")]
    public async Task<IActionResult> GetByParent(int parentId)
    {
        if (parentId != CurrentUserId)
            return Forbid();
        var result = await _parentMeetingService.GetRequestsByParentAsync(parentId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// جلب طلب معين
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _parentMeetingService.GetRequestByIdAsync(id);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

/// <summary>
/// الموافقة على طلب الاجتماع (أدمن)
/// </summary>
[Authorize(Roles = "Admin")]
[HttpPut("{id}/approve")]
public async Task<IActionResult> ApproveRequest(int id, [FromBody] ApproveMeetingRequest request)
{
    var result = await _parentMeetingService.ApproveRequestAsync(id, CurrentUserId, request.ScheduledDate);
    if (!result.IsSuccess)
        return BadRequest(result);
    return Ok(result);
}

/// <summary>
/// رفض طلب الاجتماع (أدمن)
/// </summary>
[Authorize(Roles = "Admin")]
[HttpPut("{id}/reject")]
public async Task<IActionResult> RejectRequest(int id, [FromBody] RejectMeetingRequest? request)
{
    var result = await _parentMeetingService.RejectRequestAsync(id, CurrentUserId, request?.Reason);
    if (!result.IsSuccess)
        return BadRequest(result);
    return Ok(result);
}

/// <summary>
/// إنهاء طلب الاجتماع (أدمن)
/// </summary>
[Authorize(Roles = "Admin")]
[HttpPut("{id}/complete")]
public async Task<IActionResult> CompleteRequest(int id)
{
    var result = await _parentMeetingService.CompleteRequestAsync(id, CurrentUserId);
    if (!result.IsSuccess)
        return BadRequest(result);
    return Ok(result);
}

    /// <summary>
    /// جلب كل الطلبات (للأدمن)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _parentMeetingService.GetAllRequestsAsync();
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }
}

/// <summary>
/// DTO للموافقة على طلب الاجتماع
/// </summary>
public class ApproveMeetingRequest
{
    public DateTime ScheduledDate { get; set; }
}

/// <summary>
/// DTO لرفض طلب الاجتماع
/// </summary>
public class RejectMeetingRequest
{
    public string? Reason { get; set; }
}
