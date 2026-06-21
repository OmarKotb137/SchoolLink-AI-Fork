using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.Interfaces;
using Project.Domain.Enums;

namespace Project.API.Controllers;

[Authorize(Roles = "Admin,Teacher")]
[ApiController]
[Route("api/class-analysis")]
public class ClassAnalysisController : ControllerBase
{
    private readonly IClassAnalysisService _service;

    public ClassAnalysisController(IClassAnalysisService service)
    {
        _service = service;
    }

    [HttpGet("{classId}/full")]
    public async Task<IActionResult> GetFullAnalysis(int classId, [FromQuery] AcademicTerm? term)
    {
        var result = await _service.GetFullAnalysisAsync(classId, term);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("{classId}/overview")]
    public async Task<IActionResult> GetOverview(int classId, [FromQuery] AcademicTerm? term)
    {
        var result = await _service.GetOverviewAsync(classId, term);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("{classId}/subjects")]
    public async Task<IActionResult> GetSubjectPerformance(int classId, [FromQuery] AcademicTerm? term)
    {
        var result = await _service.GetSubjectPerformanceAsync(classId, term);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("{classId}/attendance")]
    public async Task<IActionResult> GetAttendanceTrends(int classId, [FromQuery] AcademicTerm? term)
    {
        var result = await _service.GetAttendanceTrendsAsync(classId, term);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("{classId}/top-students")]
    public async Task<IActionResult> GetTopStudents(int classId, [FromQuery] int count = 10, [FromQuery] AcademicTerm? term = null)
    {
        var result = await _service.GetTopStudentsAsync(classId, count, term);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("{classId}/at-risk")]
    public async Task<IActionResult> GetAtRiskStudents(int classId, [FromQuery] AcademicTerm? term)
    {
        var result = await _service.GetAtRiskStudentsAsync(classId, term);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("{classId}/weakness")]
    public async Task<IActionResult> GetWeaknessAnalysis(int classId, [FromQuery] AcademicTerm? term)
    {
        var result = await _service.GetWeaknessAnalysisAsync(classId, term);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("{classId}/students")]
    public async Task<IActionResult> GetStudents(int classId, [FromQuery] AcademicTerm? term)
    {
        var result = await _service.GetStudentsAsync(classId, term);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }
}
