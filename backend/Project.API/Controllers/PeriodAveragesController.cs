using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.PeriodAverages;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PeriodAveragesController : ControllerBase
{
    private readonly IPeriodAverageService _service;

    public PeriodAveragesController(IPeriodAverageService service)
    {
        _service = service;
    }

    [HttpPost("calculate")]
    public async Task<IActionResult> Calculate([FromBody] CalculatePeriodAverageRequest request)
    {
        var result = await _service.CalculateAndSaveAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeletePeriodAverageAsync(id);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("by-enrollment/{enrollmentId:int}")]
    public async Task<IActionResult> GetByEnrollment(int enrollmentId)
    {
        var result = await _service.GetByEnrollmentAsync(enrollmentId);
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
}
