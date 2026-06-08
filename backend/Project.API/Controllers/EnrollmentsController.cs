using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.Enrollments;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[ApiController]
[Route("api/enrollments")]
[Authorize(Roles = "Admin,Teacher,Student,Parent")]
public class EnrollmentsController : ControllerBase
{
    private readonly IStudentEnrollmentService _enrollmentService;

    public EnrollmentsController(IStudentEnrollmentService enrollmentService)
    {
        _enrollmentService = enrollmentService;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> Enroll([FromBody] EnrollStudentRequest request)
    {
        var result = await _enrollmentService.EnrollStudentAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("bulk")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> BulkEnroll([FromBody] BulkEnrollStudentsRequest request)
    {
        var result = await _enrollmentService.BulkEnrollStudentsAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("transfer")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> Transfer([FromBody] TransferStudentRequest request)
    {
        var result = await _enrollmentService.TransferStudentAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("by-student/{studentId:int}")]
    public async Task<IActionResult> GetByStudent(int studentId)
    {
        var result = await _enrollmentService.GetEnrollmentsByStudentAsync(studentId);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("active/{studentId:int}")]
    public async Task<IActionResult> GetActive(int studentId, [FromQuery] int academicYearId)
    {
        var result = await _enrollmentService.GetActiveEnrollmentByStudentAsync(studentId, academicYearId);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("by-class/{classId:int}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> GetByClass(int classId, [FromQuery] int academicYearId, [FromQuery] bool activeOnly = true)
    {
        var result = await _enrollmentService.GetEnrollmentsByClassAsync(classId, academicYearId, activeOnly);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("transfers-history")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> GetTransferHistory([FromQuery] int academicYearId)
    {
        var result = await _enrollmentService.GetTransferHistoryAsync(academicYearId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }
}
