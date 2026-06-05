using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;

namespace Project.API.Controllers.AI;

[Route("api/ai/study-schedule")]
[ApiController]
[Authorize(Roles = "Student,Parent")]
public class StudyScheduleController : ControllerBase
{
    private readonly IStudyScheduleOptimizerService _service;

    public StudyScheduleController(IStudyScheduleOptimizerService service) => _service = service;

    [HttpPost("optimize")]
    public async Task<IActionResult> Optimize([FromBody] StudyPlanOptimizationRequest request, CancellationToken ct)
    {
        var result = await _service.OptimizeScheduleAsync(request, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpGet("recommended/{enrollmentId}")]
    public async Task<IActionResult> Recommended(int enrollmentId, CancellationToken ct)
    {
        var result = await _service.GetRecommendedScheduleAsync(enrollmentId, ct);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }
}
