using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.AssignmentSubmission;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[ApiController]
[Route("api/assignment-submissions")]
[Authorize]
public class AssignmentSubmissionsController : ControllerBase
{
    private readonly IAssignmentSubmissionService _service;

    public AssignmentSubmissionsController(IAssignmentSubmissionService service)
    {
        _service = service;
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,Teacher")]
    [HttpGet("by-assignment/{assignmentId:int}")]
    public async Task<IActionResult> GetByAssignment(int assignmentId)
    {
        var result = await _service.GetByAssignmentIdAsync(assignmentId);
        return Ok(result);
    }

    [Authorize(Roles = "Student")]
    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] CreateAssignmentSubmissionDto dto)
    {
        var result = await _service.SubmitAsync(dto);
        if (!result.IsSuccess)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    [Authorize(Roles = "Admin,Teacher")]
    [HttpPatch("{submissionId:int}/grade")]
    public async Task<IActionResult> Grade(int submissionId)
    {
        var result = await _service.GradeAsync(submissionId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,Teacher")]
    [HttpPut("grade")]
    public async Task<IActionResult> GradeSubmission([FromBody] GradeSubmissionRequest request)
    {
        var result = await _service.GradeSubmissionAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("by-student/{enrollmentId:int}")]
    public async Task<IActionResult> GetByStudent(int enrollmentId)
    {
        var result = await _service.GetByStudentAsync(enrollmentId);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,Teacher")]
    [HttpPatch("{submissionId:int}/reopen")]
    public async Task<IActionResult> Reopen(int submissionId)
    {
        var result = await _service.ReopenAsync(submissionId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }
}