using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.Enrollments;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[ApiController]
[Route("api/student-progression")]
[Authorize(Roles = "Admin")]
public class StudentProgressionController : ControllerBase
{
    private readonly IStudentProgressionService _service;

    public StudentProgressionController(IStudentProgressionService service)
    {
        _service = service;
    }

    [HttpGet("candidates")]
    public async Task<IActionResult> GetCandidates(
        [FromQuery] int gradeLevelId,
        [FromQuery] int academicYearId,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetCandidatesAsync(gradeLevelId, academicYearId, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("execute")]
    public async Task<IActionResult> Execute(
        [FromBody] StudentProgressionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.ExecuteAsync(request, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }
}
