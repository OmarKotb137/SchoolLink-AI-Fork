using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.Common;
using Project.BLL.DTOs.Enrollments;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[ApiController]
[Route("api/enrollments")]
[Authorize(Roles = "Admin")]
public class EnrollmentsController : ControllerBase
{
    private readonly IStudentEnrollmentService _enrollmentService;

    public EnrollmentsController(IStudentEnrollmentService enrollmentService)
    {
        _enrollmentService = enrollmentService;
    }

    [HttpPost]
    public async Task<IActionResult> Enroll([FromBody] EnrollStudentRequest request)
    {
        var result = await _enrollmentService.EnrollStudentAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> BulkEnroll([FromBody] BulkEnrollStudentsRequest request)
    {
        var result = await _enrollmentService.BulkEnrollStudentsAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("transfer")]
    public async Task<IActionResult> Transfer([FromBody] TransferStudentRequest request)
    {
        var result = await _enrollmentService.TransferStudentAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("transfer/bulk")]
    public async Task<IActionResult> BulkTransfer([FromBody] BulkTransferStudentsRequest request)
    {
        var result = await _enrollmentService.BulkTransferStudentsAsync(request);
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
    public async Task<IActionResult> GetByClass(int classId, [FromQuery] int academicYearId, [FromQuery] bool activeOnly = true)
    {
        var result = await _enrollmentService.GetEnrollmentsByClassAsync(classId, academicYearId, activeOnly);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("by-class/{classId:int}/paged")]
    public async Task<IActionResult> GetByClassPaged(
        int classId,
        [FromQuery] int academicYearId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool activeOnly = true,
        [FromQuery] string? searchTerm = null)
    {
        var result = await _enrollmentService.GetEnrollmentsByClassPagedAsync(classId, academicYearId, page, pageSize, activeOnly, searchTerm);
        if (!result.IsSuccess)
            return result.StatusCode == 404 ? NotFound(result) : BadRequest(result);
        return Ok(result);
    }

    [HttpGet("transfers-history")]
    public async Task<IActionResult> GetTransferHistory(
        [FromQuery] int academicYearId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _enrollmentService.GetTransferHistoryAsync(academicYearId, page, pageSize);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }
}
