using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.StudentEvaluations;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[Authorize(Roles = "Admin,Teacher")]
[ApiController]
[Route("api/[controller]")]
public class StudentEvaluationsController : ControllerBase
{
    private readonly IStudentEvaluationService _service;

    public StudentEvaluationsController(IStudentEvaluationService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Record([FromBody] RecordEvaluationRequest request)
    {
        request.EnteredById = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _service.RecordEvaluationAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetByEnrollmentAndPeriod), new { enrollmentId = request.EnrollmentId, periodId = request.PeriodId }, result);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateEvaluationRequest request)
    {
        request.UpdatedById = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _service.UpdateEvaluationAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteEvaluationAsync(id);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("by-enrollment-period")]
    public async Task<IActionResult> GetByEnrollmentAndPeriod(
        [FromQuery] int enrollmentId,
        [FromQuery] int periodId)
    {
        var result = await _service.GetByEnrollmentAndPeriodAsync(enrollmentId, periodId);
        return Ok(result);
    }

    [HttpGet("by-class-period")]
    public async Task<IActionResult> GetByClassAndPeriod(
        [FromQuery] int classId,
        [FromQuery] int periodId)
    {
        var result = await _service.GetByClassAndPeriodAsync(classId, periodId);
        return Ok(result);
    }

    [HttpPost("auto-fill-attendance")]
    public async Task<IActionResult> AutoFillAttendance(
        [FromQuery] int classId,
        [FromQuery] int periodId)
    {
        var result = await _service.AutoFillAttendanceScoresAsync(classId, periodId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }
}
