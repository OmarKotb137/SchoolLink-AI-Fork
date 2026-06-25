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

    [HttpGet("teacher-growth")]
    public async Task<IActionResult> GetTeacherGrowthDashboard(
        [FromQuery] AcademicTerm? term,
        [FromQuery] int? teacherId,
        [FromQuery] int? classId)
    {
        var result = await _service.GetTeacherGrowthDashboardAsync(term, teacherId, classId);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("teacher-growth/overview")]
    public async Task<IActionResult> GetTeacherGrowthOverview(
        [FromQuery] AcademicTerm? term,
        [FromQuery] int? teacherId,
        [FromQuery] int? classId)
    {
        var result = await _service.GetTeacherGrowthDashboardOverviewAsync(term, teacherId, classId);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("teacher-growth/teachers")]
    public async Task<IActionResult> GetTeacherGrowthTeachers(
        [FromQuery] AcademicTerm? term,
        [FromQuery] int? teacherId,
        [FromQuery] int? classId)
    {
        var result = await _service.GetTeacherGrowthDashboardTeachersAsync(term, teacherId, classId);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("teacher-growth/students")]
    public async Task<IActionResult> GetTeacherGrowthStudents(
        [FromQuery] int teacherId,
        [FromQuery] int? classId,
        [FromQuery] int? subjectId,
        [FromQuery] AcademicTerm? term,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _service.GetTeacherGrowthStudentsAsync(teacherId, classId, subjectId, term, page, pageSize);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("teacher-growth/student-weeks")]
    public async Task<IActionResult> GetStudentGrowthWeeks(
        [FromQuery] int studentId,
        [FromQuery] int? classId,
        [FromQuery] int? subjectId,
        [FromQuery] int? teacherId,
        [FromQuery] AcademicTerm? term)
    {
        var result = await _service.GetStudentGrowthWeeksAsync(studentId, classId, subjectId, teacherId, term);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("teacher-growth/student-rankings")]
    public async Task<IActionResult> GetStudentGrowthRankings([FromQuery] AcademicTerm? term)
    {
        var result = await _service.GetStudentGrowthRankingsAsync(term);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("teacher-growth/student-exams")]
    public async Task<IActionResult> GetStudentExamSummary(
        [FromQuery] int studentId,
        [FromQuery] AcademicTerm? term)
    {
        var result = await _service.GetStudentExamSummaryAsync(studentId, term);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("teacher-growth/student-final-grades")]
    public async Task<IActionResult> GetStudentFinalGrades(
        [FromQuery] int studentId,
        [FromQuery] AcademicTerm? term)
    {
        var result = await _service.GetStudentFinalGradesAsync(studentId, term);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("subject-teacher-board")]
    public async Task<IActionResult> GetClassSubjectTeacherBoard([FromQuery] AcademicTerm? term)
    {
        var result = await _service.GetClassSubjectTeacherBoardAsync(term);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }
}
