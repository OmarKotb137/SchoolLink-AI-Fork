using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.FinalGrades;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FinalGradesController : ControllerBase
{
    private readonly IFinalGradeService _service;

    public FinalGradesController(IFinalGradeService service)
    {
        _service = service;
    }

    [HttpPost("calculate-all/{classId:int}")]
    public async Task<IActionResult> CalculateAllForClass(int classId)
    {
        var result = await _service.CalculateFinalGradesForClassAsync(classId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("by-academic-year/{academicYearId:int}")]
    public async Task<IActionResult> GetByAcademicYear(int academicYearId)
    {
        var result = await _service.GetFinalGradesByAcademicYearAsync(academicYearId);
        return Ok(result);
    }

    [HttpPost("calculate/{enrollmentId:int}")]
    public async Task<IActionResult> Calculate(int enrollmentId)
    {
        var result = await _service.CalculateFinalGradeAsync(enrollmentId);
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
    public async Task<IActionResult> GetByEnrollment(int enrollmentId)
    {
        var result = await _service.GetFinalGradeByEnrollmentAsync(enrollmentId);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("by-class/{classId:int}")]
    public async Task<IActionResult> GetByClass(int classId)
    {
        var result = await _service.GetFinalGradesByClassAsync(classId);
        return Ok(result);
    }

    [HttpGet("top-students/{classId:int}")]
    public async Task<IActionResult> GetTopStudents(int classId, [FromQuery] int count = 10)
    {
        var result = await _service.GetTopStudentsAsync(classId, count);
        return Ok(result);
    }

    [HttpGet("needing-support/{classId:int}")]
    public async Task<IActionResult> GetStudentsNeedingSupport(int classId, [FromQuery] decimal threshold = 50)
    {
        var result = await _service.GetStudentsNeedingSupportAsync(classId, threshold);
        return Ok(result);
    }
}
