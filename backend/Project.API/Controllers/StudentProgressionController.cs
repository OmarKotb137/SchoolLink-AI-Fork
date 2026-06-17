using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.Enrollments;
using Project.BLL.Interfaces;
using Project.Domain.Enums;

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
        [FromQuery] AcademicTerm? term = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.GetCandidatesAsync(gradeLevelId, academicYearId, term, cancellationToken);
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
