using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.PeriodicAssessments;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PeriodicAssessmentsController : ControllerBase
{
    private readonly IPeriodicAssessmentService _service;

    public PeriodicAssessmentsController(IPeriodicAssessmentService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Record([FromBody] RecordPeriodicAssessmentRequest request)
    {
        var result = await _service.RecordPeriodicAssessmentAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetByEnrollment), new { enrollmentId = request.EnrollmentId }, result);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdatePeriodicAssessmentRequest request)
    {
        var result = await _service.UpdatePeriodicAssessmentAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeletePeriodicAssessmentAsync(id);
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
}
