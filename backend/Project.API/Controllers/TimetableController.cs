using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[ApiController]
[Route("api/timetables")]
[Authorize]
public class TimetableController : ControllerBase
{
    private readonly ITimetableService _timetableService;
    private readonly IAcademicYearService _academicYearService;
    private readonly IClassService _classService;

    public TimetableController(
        ITimetableService timetableService,
        IAcademicYearService academicYearService,
        IClassService classService)
    {
        _timetableService = timetableService;
        _academicYearService = academicYearService;
        _classService = classService;
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTimetableRequest request)
    {
        var result = await _timetableService.CreateTimetableAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    [Authorize(Roles = "Admin,Teacher")]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        if (!User.IsInRole("Admin"))
        {
            var currentYearResult = await _academicYearService.GetCurrentAcademicYearAsync();
            if (!currentYearResult.IsSuccess || currentYearResult.Data is null)
                return NotFound(currentYearResult);

            var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var teacherClassesResult = await _classService.GetClassesByTeacherAsync(teacherId, currentYearResult.Data.Id);
            if (!teacherClassesResult.IsSuccess)
                return BadRequest(teacherClassesResult);

            var timetableResult = await _timetableService.GetTimetableByIdAsync(id);
            if (!timetableResult.IsSuccess)
                return NotFound(timetableResult);

            if (!(teacherClassesResult.Data?.Any(c => c.Id == timetableResult.Data!.ClassId) ?? false))
                return Forbid();

            return Ok(timetableResult);
        }

        var result = await _timetableService.GetTimetableByIdAsync(id);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,Teacher")]
    [HttpGet("by-class")]
    public async Task<IActionResult> GetByClass([FromQuery] int classId, [FromQuery] int academicYearId)
    {
        if (!User.IsInRole("Admin"))
        {
            var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var teacherClassesResult = await _classService.GetClassesByTeacherAsync(teacherId, academicYearId);
            if (!teacherClassesResult.IsSuccess)
                return BadRequest(teacherClassesResult);

            if (!(teacherClassesResult.Data?.Any(c => c.Id == classId) ?? false))
                return Forbid();
        }

        var result = await _timetableService.GetTimetablesByClassAndYearAsync(classId, academicYearId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,Teacher,Parent,Student")]
    [HttpGet("active/by-class")]
    public async Task<IActionResult> GetActiveByClass([FromQuery] int classId, [FromQuery] int academicYearId)
    {
        if (User.IsInRole("Teacher"))
        {
            var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var teacherClassesResult = await _classService.GetClassesByTeacherAsync(teacherId, academicYearId);
            if (!teacherClassesResult.IsSuccess)
                return BadRequest(teacherClassesResult);

            if (!(teacherClassesResult.Data?.Any(c => c.Id == classId) ?? false))
                return Forbid();
        }

        var result = await _timetableService.GetByClassAsync(classId, academicYearId);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("student/{enrollmentId:int}")]
    public async Task<IActionResult> GetByStudent(int enrollmentId)
    {
        if (User.IsInRole("Admin"))
        {
            var adminResult = await _timetableService.GetByStudentAsync(enrollmentId);
            if (!adminResult.IsSuccess)
                return NotFound(adminResult);
            return Ok(adminResult);
        }

        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _timetableService.GetByStudentForUserAsync(enrollmentId, currentUserId);
        if (!result.IsSuccess)
            return Forbid();
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("teacher-schedule/{teacherId:int}")]
    public async Task<IActionResult> GetTeacherSchedule(int teacherId, [FromQuery] int academicYearId)
    {
        if (!User.IsInRole("Admin"))
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            if (currentUserId != teacherId)
                return Forbid();
        }

        var result = await _timetableService.GetTeacherScheduleAsync(teacherId, academicYearId);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [Authorize(Roles = "Teacher")]
    [HttpGet("my-schedule")]
    public async Task<IActionResult> GetMySchedule([FromQuery] int academicYearId)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _timetableService.GetTeacherScheduleAsync(currentUserId, academicYearId);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [Authorize(Roles = "Teacher")]
    [HttpGet("my-schedule/current-year")]
    public async Task<IActionResult> GetMyScheduleCurrentYear()
    {
        var currentYearResult = await _academicYearService.GetCurrentAcademicYearAsync();
        if (!currentYearResult.IsSuccess || currentYearResult.Data is null)
            return NotFound(currentYearResult);

        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _timetableService.GetTeacherScheduleAsync(currentUserId, currentYearResult.Data.Id);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [Authorize(Roles = "Student")]
    [HttpGet("my-student-schedule")]
    public async Task<IActionResult> GetMyStudentSchedule([FromQuery] int academicYearId)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _timetableService.GetMyStudentScheduleAsync(currentUserId, academicYearId);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [Authorize(Roles = "Student")]
    [HttpGet("my-student-schedule/current-year")]
    public async Task<IActionResult> GetMyStudentScheduleCurrentYear()
    {
        var currentYearResult = await _academicYearService.GetCurrentAcademicYearAsync();
        if (!currentYearResult.IsSuccess || currentYearResult.Data is null)
            return NotFound(currentYearResult);

        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _timetableService.GetMyStudentScheduleAsync(currentUserId, currentYearResult.Data.Id);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [Authorize(Roles = "Parent")]
    [HttpGet("my-child-schedules")]
    public async Task<IActionResult> GetMyChildSchedules([FromQuery] int academicYearId)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _timetableService.GetMyChildSchedulesAsync(currentUserId, academicYearId);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [Authorize(Roles = "Parent")]
    [HttpGet("my-child-schedules/current-year")]
    public async Task<IActionResult> GetMyChildSchedulesCurrentYear()
    {
        var currentYearResult = await _academicYearService.GetCurrentAcademicYearAsync();
        if (!currentYearResult.IsSuccess || currentYearResult.Data is null)
            return NotFound(currentYearResult);

        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _timetableService.GetMyChildSchedulesAsync(currentUserId, currentYearResult.Data.Id);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:int}/activate")]
    public async Task<IActionResult> Activate(int id)
    {
        var result = await _timetableService.ActivateTimetableAsync(id);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var result = await _timetableService.DeactivateTimetableAsync(id);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _timetableService.DeleteTimetableAsync(id);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("slots")]
    public async Task<IActionResult> AddSlot([FromBody] AddTimetableSlotRequest request)
    {
        var result = await _timetableService.AddTimetableSlotAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("slots/{slotId:int}")]
    public async Task<IActionResult> UpdateSlot(int slotId, [FromBody] UpdateTimetableSlotRequest request)
    {
        if (slotId != request.SlotId)
            return BadRequest("Route id does not match body id.");

        var result = await _timetableService.UpdateTimetableSlotAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("slots/{slotId:int}")]
    public async Task<IActionResult> DeleteSlot(int slotId)
    {
        var result = await _timetableService.DeleteTimetableSlotAsync(slotId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }
}
