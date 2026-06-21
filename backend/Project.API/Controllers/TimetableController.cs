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

    // ══════════════════════════════════════════════════════════════════════════
    // Helpers — safe claim extraction + status code mapping
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// يقرأ معرّف المستخدم من الـ claims بشكل آمن (بدون int.Parse الذي قد يرمي استثناء).
    /// </summary>
    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    /// <summary>
    /// يحوّل نتيجة العملية إلى HTTP response موحّد:
    ///   - نجاح             → 200 OK
    ///   - StatusCode == 404 → 404 NotFound
    ///   - StatusCode == 403 → 403
    ///   - غير ذلك           → 400 BadRequest
    /// </summary>
    private IActionResult MapResult(Common.Results.OperationResult result)
    {
        if (result.IsSuccess) return Ok(result);
        return result.StatusCode switch
        {
            404 => NotFound(result),
            403 => StatusCode(403, result),
            _   => BadRequest(result),
        };
    }

    private IActionResult MapResult<T>(Common.Results.OperationResult<T> result)
    {
        if (result.IsSuccess) return Ok(result);
        return result.StatusCode switch
        {
            404 => NotFound(result),
            403 => StatusCode(403, result),
            _   => BadRequest(result),
        };
    }

    /// <summary>
    /// تحقق أن المعلم يدرّس الفصل المطلوب. يرجع null إذا سمح، أو IActionResult للرفض.
    /// </summary>
    private async Task<IActionResult?> EnsureTeacherOwnsClassAsync(int classId, int academicYearId, CancellationToken ct)
    {
        var teacherId = GetCurrentUserId();
        if (teacherId is null)
            return Unauthorized();

        var teacherClassesResult = await _classService.GetClassesByTeacherAsync(teacherId.Value, academicYearId);
        if (!teacherClassesResult.IsSuccess)
            return BadRequest(teacherClassesResult);

        var ownsClass = teacherClassesResult.Data?.Any(c => c.Id == classId) ?? false;
        return ownsClass ? null : Forbid();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Admin: Create / Clone / Validate / Activate / Deactivate / Delete
    // ══════════════════════════════════════════════════════════════════════════

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTimetableRequest request, CancellationToken ct)
    {
        var result = await _timetableService.CreateTimetableAsync(request, ct);
        if (!result.IsSuccess)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("clone-draft")]
    public async Task<IActionResult> CloneDraft(
        [FromQuery] int classId,
        [FromQuery] int academicYearId,
        CancellationToken ct,
        [FromQuery] bool replaceExisting = false)
    {
        var result = await _timetableService.CloneDraftTimetableAsync(classId, academicYearId, replaceExisting, ct);
        return MapResult(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{id:int}/validate")]
    public async Task<IActionResult> Validate(int id, CancellationToken ct)
    {
        var result = await _timetableService.ValidateTimetableAsync(id, ct);
        return MapResult(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:int}/activate")]
    public async Task<IActionResult> Activate(int id, CancellationToken ct)
    {
        var result = await _timetableService.ActivateTimetableAsync(id, ct);
        return MapResult(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        var result = await _timetableService.DeactivateTimetableAsync(id, ct);
        return MapResult(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _timetableService.DeleteTimetableAsync(id, ct);
        return MapResult(result);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Admin / Teacher: GetById / GetByClass / GetActiveByClass
    // ══════════════════════════════════════════════════════════════════════════

    [Authorize(Roles = "Admin,Teacher")]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        // المعلم يرى فقط جداول فصوله؛ الأدمن يرى الكل.
        if (!User.IsInRole("Admin"))
        {
            var currentYearResult = await _academicYearService.GetCurrentAcademicYearAsync();
            if (!currentYearResult.IsSuccess || currentYearResult.Data is null)
                return NotFound(currentYearResult);

            var teacherId = GetCurrentUserId();
            if (teacherId is null)
                return Unauthorized();

            var teacherClassesResult = await _classService.GetClassesByTeacherAsync(teacherId.Value, currentYearResult.Data.Id);
            if (!teacherClassesResult.IsSuccess)
                return BadRequest(teacherClassesResult);

            var timetableResult = await _timetableService.GetTimetableByIdAsync(id, ct);
            if (!timetableResult.IsSuccess)
                return NotFound(timetableResult);

            if (!(teacherClassesResult.Data?.Any(c => c.Id == timetableResult.Data!.ClassId) ?? false))
                return Forbid();

            return Ok(timetableResult);
        }

        var result = await _timetableService.GetTimetableByIdAsync(id, ct);
        return MapResult(result);
    }

    [Authorize(Roles = "Admin,Teacher,Student")]
    [HttpGet("by-class")]
    public async Task<IActionResult> GetByClass([FromQuery] int classId, [FromQuery] int academicYearId, CancellationToken ct)
    {
        if (User.IsInRole("Teacher"))
        {
            var deny = await EnsureTeacherOwnsClassAsync(classId, academicYearId, ct);
            if (deny is not null) return deny;
        }

        var result = await _timetableService.GetTimetablesByClassAndYearAsync(classId, academicYearId, ct);
        return MapResult(result);
    }

    [Authorize(Roles = "Admin,Teacher,Parent,Student")]
    [HttpGet("active/by-class")]
    public async Task<IActionResult> GetActiveByClass([FromQuery] int classId, [FromQuery] int academicYearId, CancellationToken ct)
    {
        if (User.IsInRole("Teacher"))
        {
            var deny = await EnsureTeacherOwnsClassAsync(classId, academicYearId, ct);
            if (deny is not null) return deny;
        }

        var result = await _timetableService.GetByClassAsync(classId, academicYearId, ct);
        return MapResult(result);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Student / Parent / Teacher schedule reads
    // ══════════════════════════════════════════════════════════════════════════

    [Authorize(Roles = "Admin,Student,Parent")]
    [HttpGet("student/{enrollmentId:int}")]
    public async Task<IActionResult> GetByStudent(int enrollmentId, CancellationToken ct)
    {
        if (User.IsInRole("Admin"))
        {
            var adminResult = await _timetableService.GetByStudentAsync(enrollmentId, ct);
            return MapResult(adminResult);
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized();

        var result = await _timetableService.GetByStudentForUserAsync(enrollmentId, currentUserId.Value, ct);
        // GetByStudentForUserAsync يرجع 403 داخليًا عند الرفض — نحوّله لـ Forbid().
        if (!result.IsSuccess && result.StatusCode == 403)
            return Forbid();
        return MapResult(result);
    }

    [Authorize(Roles = "Admin,Teacher")]
    [HttpGet("teacher-schedule/{teacherId:int}")]
    public async Task<IActionResult> GetTeacherSchedule(int teacherId, [FromQuery] int academicYearId, CancellationToken ct)
    {
        if (!User.IsInRole("Admin"))
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId is null)
                return Unauthorized();
            if (currentUserId != teacherId)
                return Forbid();
        }

        var result = await _timetableService.GetTeacherScheduleAsync(teacherId, academicYearId, ct);
        return MapResult(result);
    }

    [Authorize(Roles = "Teacher")]
    [HttpGet("my-schedule")]
    public async Task<IActionResult> GetMySchedule([FromQuery] int academicYearId, CancellationToken ct)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized();

        var result = await _timetableService.GetTeacherScheduleAsync(currentUserId.Value, academicYearId, ct);
        return MapResult(result);
    }

    [Authorize(Roles = "Teacher")]
    [HttpGet("my-schedule/current-year")]
    public async Task<IActionResult> GetMyScheduleCurrentYear(CancellationToken ct)
    {
        var currentYearResult = await _academicYearService.GetCurrentAcademicYearAsync();
        if (!currentYearResult.IsSuccess || currentYearResult.Data is null)
            return NotFound(currentYearResult);

        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized();

        var result = await _timetableService.GetTeacherScheduleAsync(currentUserId.Value, currentYearResult.Data.Id, ct);
        return MapResult(result);
    }

    [Authorize(Roles = "Student")]
    [HttpGet("my-student-schedule")]
    public async Task<IActionResult> GetMyStudentSchedule([FromQuery] int academicYearId, CancellationToken ct)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized();

        var result = await _timetableService.GetMyStudentScheduleAsync(currentUserId.Value, academicYearId, ct);
        return MapResult(result);
    }

    [Authorize(Roles = "Student")]
    [HttpGet("my-student-schedule/current-year")]
    public async Task<IActionResult> GetMyStudentScheduleCurrentYear(CancellationToken ct)
    {
        var currentYearResult = await _academicYearService.GetCurrentAcademicYearAsync();
        if (!currentYearResult.IsSuccess || currentYearResult.Data is null)
            return NotFound(currentYearResult);

        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized();

        var result = await _timetableService.GetMyStudentScheduleAsync(currentUserId.Value, currentYearResult.Data.Id, ct);
        return MapResult(result);
    }

    [Authorize(Roles = "Parent")]
    [HttpGet("my-child-schedules")]
    public async Task<IActionResult> GetMyChildSchedules([FromQuery] int academicYearId, CancellationToken ct)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized();

        var result = await _timetableService.GetMyChildSchedulesAsync(currentUserId.Value, academicYearId, ct);
        return MapResult(result);
    }

    [Authorize(Roles = "Parent")]
    [HttpGet("my-child-schedules/current-year")]
    public async Task<IActionResult> GetMyChildSchedulesCurrentYear(CancellationToken ct)
    {
        var currentYearResult = await _academicYearService.GetCurrentAcademicYearAsync();
        if (!currentYearResult.IsSuccess || currentYearResult.Data is null)
            return NotFound(currentYearResult);

        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized();

        var result = await _timetableService.GetMyChildSchedulesAsync(currentUserId.Value, currentYearResult.Data.Id, ct);
        return MapResult(result);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Admin: Slots CRUD
    // ══════════════════════════════════════════════════════════════════════════

    [Authorize(Roles = "Admin")]
    [HttpPost("slots")]
    public async Task<IActionResult> AddSlot([FromBody] AddTimetableSlotRequest request, CancellationToken ct)
    {
        var result = await _timetableService.AddTimetableSlotAsync(request, ct);
        return MapResult(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("slots/{slotId:int}")]
    public async Task<IActionResult> UpdateSlot(int slotId, [FromBody] UpdateTimetableSlotRequest request, CancellationToken ct)
    {
        if (slotId != request.SlotId)
            return BadRequest(Common.Results.OperationResult.Failure("معرّف الرابط لا يطابق معرّف الطلب."));

        var result = await _timetableService.UpdateTimetableSlotAsync(request, ct);
        return MapResult(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("slots/{slotId:int}")]
    public async Task<IActionResult> DeleteSlot(int slotId, CancellationToken ct)
    {
        var result = await _timetableService.DeleteTimetableSlotAsync(slotId, ct);
        return MapResult(result);
    }

    /// <summary>
    /// تحديث توقيت كل الحصص برقم حصة معيّن داخل الجدول دفعة واحدة (batch).
    /// يُستخدم من رأس الجدول الموحّد في واجهة الأدمن لتطبيق التوقيت على عمود كامل.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}/period-time")]
    public async Task<IActionResult> UpdatePeriodTiming(int id, [FromBody] UpdatePeriodTimingRequest request, CancellationToken ct)
    {
        var result = await _timetableService.UpdatePeriodTimingAsync(id, request, ct);
        return MapResult(result);
    }
}
