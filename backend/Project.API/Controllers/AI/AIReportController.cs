using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.AI.Interfaces;
using Project.BLL.Interfaces;
using Project.Domain.Enums;

namespace Project.API.Controllers.AI;

[Route("api/ai/reports")]
[ApiController]
[Authorize(Roles = "Admin,Teacher,Parent")]
public class AIReportController : ControllerBase
{
    private readonly IAIReportService _service;
    private readonly IAcademicYearService _academicYearService;

    public AIReportController(
        IAIReportService service,
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

    [HttpGet("student/{studentId}/history")]
    public async Task<IActionResult> StudentHistory(int studentId)
    {
        var result = await _service.GetStudentReportsAsync(studentId);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    [HttpGet("{reportId}")]
    public async Task<IActionResult> GetReport(int reportId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var roleStr = User.FindFirst(ClaimTypes.Role)?.Value;

        if (!Enum.TryParse<UserRole>(roleStr, out var role))
            return Unauthorized();

        var result = await _service.GetReportByIdAsync(reportId, userId, role);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    // ---------------------------------------------------------------
    //  New structured endpoints
    // ---------------------------------------------------------------

    /// <summary>
    /// Returns structured student report with scores, metrics, and AI analysis
    /// </summary>
    [HttpGet("student/{studentId}/period/{periodId}/structured")]
    public async Task<IActionResult> StructuredStudentReport(int studentId, int periodId, CancellationToken ct)
    {
        var term = await ResolveCurrentTermAsync();
        var result = await _service.GetStructuredStudentReportAsync(studentId, periodId, term, ct);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Returns structured recommendations with parsed items and AI-generated text
    /// </summary>
    [HttpGet("recommendations/{studentId}/structured")]
    public async Task<IActionResult> StructuredRecommendations(int studentId, CancellationToken ct)
    {
        var term = await ResolveCurrentTermAsync();
        var result = await _service.GetStructuredRecommendationsAsync(studentId, term, ct);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Soft-deletes a report by ID
    /// </summary>
    [HttpDelete("{reportId}")]
    public async Task<IActionResult> DeleteReport(int reportId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var roleStr = User.FindFirst(ClaimTypes.Role)?.Value;

        if (!Enum.TryParse<UserRole>(roleStr, out var role))
            return Unauthorized();

        var result = await _service.DeleteReportAsync(reportId, userId, role);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }
}
