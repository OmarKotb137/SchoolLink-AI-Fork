using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.AI.Interfaces;

namespace Project.API.Controllers.AI;

[Route("api/ai/evaluation-report")]
[ApiController]
[Authorize(Roles = "Admin,Teacher,Parent")]
public class EvaluationReportController : ControllerBase
{
    private readonly IEvaluationReportService _service;

    public EvaluationReportController(IEvaluationReportService service) => _service = service;

    [HttpGet("student/{studentId}/period/{periodId}")]
    public async Task<IActionResult> StudentReport(int studentId, int periodId, CancellationToken ct)
    {
        var result = await _service.GenerateStudentReportAsync(studentId, periodId, ct);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    [HttpGet("class/{classId}/period/{periodId}")]
    public async Task<IActionResult> ClassReport(int classId, int periodId, CancellationToken ct)
    {
        var result = await _service.GenerateClassReportAsync(classId, periodId, ct);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    [HttpGet("recommendations/{studentId}")]
    public async Task<IActionResult> Recommendations(int studentId, CancellationToken ct)
    {
        var result = await _service.GenerateRecommendationsAsync(studentId, ct);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }
}
