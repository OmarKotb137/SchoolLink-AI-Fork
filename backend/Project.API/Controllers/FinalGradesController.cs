using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.FinalGrades;
using Project.BLL.Interfaces;
using Project.Domain.Enums;

namespace Project.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Teacher,Student,Parent")]
public class FinalGradesController : ControllerBase
{
    private readonly IFinalGradeService _service;

    public FinalGradesController(IFinalGradeService service)
    {
        _service = service;
    }

    [HttpPost("calculate-all/{classId:int}")]
    public async Task<IActionResult> CalculateAllForClass(int classId, [FromQuery] AcademicTerm? term = null)
    {
        var result = await _service.CalculateFinalGradesForClassAsync(classId, term);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("calculate-full/{classId:int}")]
    public async Task<IActionResult> CalculateFullForClass(int classId, [FromBody] CalculateFullFinalGradesRequest request)
    {
        var result = await _service.CalculateFullForClassAsync(classId, request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("recalculate/{classId:int}")]
    public async Task<IActionResult> RecalculateForClass(int classId, [FromQuery] AcademicTerm? term = null, [FromQuery] int? subjectId = null)
    {
        var result = await _service.RecalculateForClassAsync(classId, term, subjectId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("by-academic-year/{academicYearId:int}")]
    public async Task<IActionResult> GetByAcademicYear(int academicYearId, [FromQuery] AcademicTerm? term = null)
    {
        var result = await _service.GetFinalGradesByAcademicYearAsync(academicYearId, term);
        return Ok(result);
    }

    [HttpPost("calculate/{enrollmentId:int}")]
    public async Task<IActionResult> Calculate(int enrollmentId, [FromQuery] AcademicTerm? term = null)
    {
        var result = await _service.CalculateFinalGradeAsync(enrollmentId, term);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("publish")]
    public async Task<IActionResult> Publish([FromBody] PublishGradesRequest request)
    {
        request.PublishedById = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _service.PublishGradesAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("by-enrollment/{enrollmentId:int}")]
    public async Task<IActionResult> GetByEnrollment(int enrollmentId, [FromQuery] AcademicTerm? term = null)
    {
        var result = await _service.GetFinalGradeByEnrollmentAsync(enrollmentId, term);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("by-class/{classId:int}")]
    public async Task<IActionResult> GetByClass(int classId, [FromQuery] AcademicTerm? term = null, [FromQuery] int? subjectId = null)
    {
        var result = await _service.GetFinalGradesByClassAsync(classId, term, subjectId);
        return Ok(result);
    }

    [HttpGet("top-students/{classId:int}")]
    public async Task<IActionResult> GetTopStudents(int classId, [FromQuery] int count = 10, [FromQuery] AcademicTerm? term = null)
    {
        var result = await _service.GetTopStudentsAsync(classId, count, term);
        return Ok(result);
    }

    [HttpGet("needing-support/{classId:int}")]
    public async Task<IActionResult> GetStudentsNeedingSupport(int classId, [FromQuery] decimal threshold = 50, [FromQuery] AcademicTerm? term = null)
    {
        var result = await _service.GetStudentsNeedingSupportAsync(classId, threshold, term);
        return Ok(result);
    }
}
