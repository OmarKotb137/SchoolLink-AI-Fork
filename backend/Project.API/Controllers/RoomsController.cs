using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs;
using Project.BLL.Interfaces;
using SchoolLink.Domain.Enums;

namespace Project.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class RoomsController : ControllerBase
{
    private readonly IRoomService _roomService;

    public RoomsController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    /// <summary>
    /// جلب كل الغرف مرتبة حسب النوع ثم الاسم.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _roomService.GetAllRoomsAsync();
        return Ok(result);
    }

    /// <summary>
    /// جلب غرفة بـ ID.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _roomService.GetRoomByIdAsync(id);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    /// <summary>
    /// إنشاء غرفة جديدة.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoomRequest request)
    {
        var result = await _roomService.CreateRoomAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    /// <summary>
    /// تعديل غرفة موجودة.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRoomRequest request)
    {
        if (id != request.Id)
            return BadRequest("معرف الغرفة في الرابط لا يطابق المعرف في الطلب");

        var result = await _roomService.UpdateRoomAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// حذف غرفة (حذف ناعم — لا يسمح إن كانت مستخدمة في جدول).
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _roomService.DeleteRoomAsync(id);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// جلب الغرف المتاحة ليوم وحصة معينين، مع فلتر اختياري حسب النوع.
    /// </summary>
    [HttpGet("available")]
    public async Task<IActionResult> GetAvailable(
        [FromQuery] SchoolDay day,
        [FromQuery] int periodNumber,
        [FromQuery] RoomType? type = null)
    {
        var result = await _roomService.GetAvailableRoomsAsync(day, periodNumber, type);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }
}
