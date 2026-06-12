using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs;
using Project.BLL.DTOs.Users;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[ApiController]
[Route("api/class-subject-teachers")]
[Authorize]
public class ClassSubjectTeacherController : ControllerBase
{
    private readonly IClassSubjectTeacherService _classSubjectTeacherService;
    private readonly IAcademicYearService _academicYearService;

    public ClassSubjectTeacherController(
        IClassSubjectTeacherService classSubjectTeacherService,
        IAcademicYearService academicYearService)
    {
        _classSubjectTeacherService = classSubjectTeacherService;
        _academicYearService = academicYearService;
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
            var assignmentsResult = await _classSubjectTeacherService.GetByTeacherAsync(teacherId, currentYearResult.Data.Id);
            if (!assignmentsResult.IsSuccess)
                return BadRequest(assignmentsResult);

            if (!(assignmentsResult.Data?.Any(a => a.Id == id) ?? false))
                return Forbid();
        }

        var result = await _classSubjectTeacherService.GetAssignmentByIdAsync(id);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,Teacher")]
    [HttpGet("by-class/{classId:int}")]
    public async Task<IActionResult> GetByClass(int classId, [FromQuery] int academicYearId)
    {
        if (!User.IsInRole("Admin"))
        {
            var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var assignmentsResult = await _classSubjectTeacherService.GetByTeacherAsync(teacherId, academicYearId);
            if (!assignmentsResult.IsSuccess)
                return BadRequest(assignmentsResult);

            if (!(assignmentsResult.Data?.Any(a => a.ClassId == classId) ?? false))
                return Forbid();
        }

        var result = await _classSubjectTeacherService.GetByClassAsync(classId, academicYearId);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("by-teacher/{teacherId:int}")]
    public async Task<IActionResult> GetByTeacher(int teacherId, [FromQuery] int academicYearId)
    {
        if (!User.IsInRole("Admin"))
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            if (currentUserId != teacherId)
                return Forbid();
        }

        var result = await _classSubjectTeacherService.GetByTeacherAsync(teacherId, academicYearId);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [Authorize(Roles = "Teacher")]
    [HttpGet("my-assignments")]
    public async Task<IActionResult> GetMyAssignments([FromQuery] int academicYearId)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _classSubjectTeacherService.GetByTeacherAsync(currentUserId, academicYearId);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [Authorize(Roles = "Teacher")]
    [HttpGet("my-assignments/current-year")]
    public async Task<IActionResult> GetMyAssignmentsCurrentYear()
    {
        var currentYearResult = await _academicYearService.GetCurrentAcademicYearAsync();
        if (!currentYearResult.IsSuccess || currentYearResult.Data is null)
            return NotFound(currentYearResult);

        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _classSubjectTeacherService.GetByTeacherAsync(currentUserId, currentYearResult.Data.Id);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,Teacher,Student")]
    [HttpGet("by-class-public/{classId:int}")]
    public async Task<IActionResult> GetByClassPublic(int classId, [FromQuery] int academicYearId)
    {
        var result = await _classSubjectTeacherService.GetByClassAsync(classId, academicYearId);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("available-teachers")]
    public async Task<IActionResult> GetAvailableTeachers([FromQuery] int subjectId, [FromQuery] int classId, [FromQuery] int academicYearId)
    {
        var result = await _classSubjectTeacherService.GetAvailableTeachersForSubjectAsync(subjectId, classId, academicYearId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Assign([FromBody] AssignTeacherRequest request)
    {
        var result = await _classSubjectTeacherService.AssignTeacherAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("bulk")]
    public async Task<IActionResult> BulkAssign([FromBody] List<AssignTeacherRequest> requests)
    {
        var result = await _classSubjectTeacherService.BulkAssignTeachersAsync(requests);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateAssignment(int id, [FromBody] UpdateTeacherAssignmentRequest request)
    {
        if (id != request.AssignmentId)
            return BadRequest("معرّف الرابط لا يطابق معرّف الطلب.");

        var result = await _classSubjectTeacherService.UpdateTeacherAssignmentAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Unassign(int id)
    {
        var result = await _classSubjectTeacherService.UnassignTeacherAsync(id);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }
}
