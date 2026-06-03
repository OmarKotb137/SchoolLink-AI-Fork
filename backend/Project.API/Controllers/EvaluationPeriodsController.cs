using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.EvaluationPeriods;
using Project.BLL.Interfaces;
using Project.Domain.Enums;

namespace Project.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EvaluationPeriodsController : ControllerBase
{
    private readonly IEvaluationPeriodService _service;

    public EvaluationPeriodsController(IEvaluationPeriodService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEvaluationPeriodRequest request)
    {
        var result = await _service.CreateEvaluationPeriodAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetByAcademicYear), new { academicYearId = request.AcademicYearId }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEvaluationPeriodRequest request)
    {
        if (id != request.Id)
            return BadRequest("معرف الفترة في الرابط لا يطابق المعرف في الطلب");
        var result = await _service.UpdateEvaluationPeriodAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteEvaluationPeriodAsync(id);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("by-academic-year/{academicYearId:int}")]
    public async Task<IActionResult> GetByAcademicYear(int academicYearId, [FromQuery] PeriodType? type = null)
    {
        var result = await _service.GetPeriodsByAcademicYearAsync(academicYearId, type);
        return Ok(result);
    }

    [HttpGet("current-week/{academicYearId:int}")]
    public async Task<IActionResult> GetCurrentWeek(int academicYearId)
    {
        var result = await _service.GetCurrentWeekAsync(academicYearId);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("month-names/{academicYearId:int}")]
    public async Task<IActionResult> GetDistinctMonthNames(int academicYearId)
    {
        var result = await _service.GetDistinctMonthNamesAsync(academicYearId);
        return Ok(result);
    }

    [HttpGet("by-month/{academicYearId:int}")]
    public async Task<IActionResult> GetByMonth(int academicYearId, [FromQuery] string monthName)
    {
        var result = await _service.GetPeriodsByMonthAsync(academicYearId, monthName);
        return Ok(result);
    }
}
