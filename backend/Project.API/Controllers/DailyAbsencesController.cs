using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.DailyAbsences;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DailyAbsencesController : ControllerBase
{
    private readonly IDailyAbsenceService _service;

    public DailyAbsencesController(IDailyAbsenceService service)
    {
        _service = service;
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetAbsenceByIdAsync(id);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("by-class/{classId:int}")]
    public async Task<IActionResult> GetByClass(int classId, [FromQuery] DateOnly date)
    {
        var result = await _service.GetAbsencesByClassAsync(classId, date);
        return Ok(result);
    }

    [HttpGet("by-date-range")]
    public async Task<IActionResult> GetByDateRange(
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly toDate,
        [FromQuery] int? classSubjectTeacherId = null)
    {
        var result = await _service.GetAbsencesByDateRangeAsync(fromDate, toDate, classSubjectTeacherId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Record([FromBody] RecordAbsenceRequest request)
    {
        request.RecordedById = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _service.RecordAbsenceAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetByEnrollment), new { enrollmentId = request.EnrollmentId }, result);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateAbsenceRequest request)
    {
        var result = await _service.UpdateAbsenceAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAbsenceAsync(id);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("by-enrollment/{enrollmentId:int}")]
    public async Task<IActionResult> GetByEnrollment(
        int enrollmentId,
        [FromQuery] int? classSubjectTeacherId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate)
    {
        var filter = new GetAbsenceFilter
        {
            EnrollmentId = enrollmentId,
            ClassSubjectTeacherId = classSubjectTeacherId,
            FromDate = fromDate,
            ToDate = toDate
        };
        var result = await _service.GetAbsencesByEnrollmentAsync(filter);
        return Ok(result);
    }

    [HttpGet("summary/{enrollmentId:int}")]
    public async Task<IActionResult> GetSummary(int enrollmentId, [FromQuery] int? classSubjectTeacherId)
    {
        var result = await _service.GetAbsenceSummaryAsync(enrollmentId, classSubjectTeacherId);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }
}
