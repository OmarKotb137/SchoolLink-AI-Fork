using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.AI.Interfaces;
using Project.BLL.Interfaces;
using Project.Domain.Enums;

namespace Project.API.Controllers.AI;

[Route("api/ai/evaluation-report")]
[ApiController]
[Authorize(Roles = "Admin,Teacher,Parent")]
public class EvaluationReportController : ControllerBase
{
    private readonly IEvaluationReportService _service;
    private readonly IAcademicYearService _academicYearService;

    public EvaluationReportController(
        IEvaluationReportService service,
        IAcademicYearService academicYearService)
    {
        _service = service;
        _academicYearService = academicYearService;
    }

    private async Task<AcademicTerm?> ResolveCurrentTermAsync()
    {
        var result = await _academicYearService.GetCurrentTermAsync();
        return result.IsSuccess ? result.Data : null;
    }

    [HttpGet("student/{studentId}/period/{periodId}")]
    public async Task<IActionResult> StudentReport(int studentId, int periodId, CancellationToken ct)
    {
        var term = await ResolveCurrentTermAsync();
        var result = await _service.GenerateStudentReportAsync(studentId, periodId, term, ct);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    [HttpGet("class/{classId}/period/{periodId}")]
    public async Task<IActionResult> ClassReport(int classId, int periodId, CancellationToken ct)
    {
        var term = await ResolveCurrentTermAsync();
        var result = await _service.GenerateClassReportAsync(classId, periodId, term, ct);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    [HttpGet("recommendations/{studentId}")]
    public async Task<IActionResult> Recommendations(int studentId, CancellationToken ct)
    {
        var term = await ResolveCurrentTermAsync();
        var result = await _service.GenerateRecommendationsAsync(studentId, term, ct);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }
}
