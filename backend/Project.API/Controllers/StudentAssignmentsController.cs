using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.StudentAssignments;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[Authorize(Roles = "Student")]
[ApiController]
[Route("api/student/assignments")]
public class StudentAssignmentsController : ControllerBase
{
    private readonly IStudentAssignmentService _studentAssignmentService;

    public StudentAssignmentsController(IStudentAssignmentService studentAssignmentService)
    {
        _studentAssignmentService = studentAssignmentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyAssignments([FromQuery] string? status = null, [FromQuery] int? subjectId = null)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _studentAssignmentService.GetMyAssignmentsAsync(userId, status, subjectId);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpGet("{assignmentId:int}")]
    public async Task<IActionResult> GetAssignmentDetails(int assignmentId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _studentAssignmentService.GetMyAssignmentDetailsAsync(userId, assignmentId);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost("{assignmentId:int}/submit")]
    public async Task<IActionResult> Submit(int assignmentId, [FromBody] SubmitStudentAssignmentDto dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _studentAssignmentService.SubmitAssignmentAsync(userId, assignmentId, dto);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpGet("/api/student/assignment-submissions/{submissionId:int}")]
    public async Task<IActionResult> GetSubmissionResult(int submissionId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _studentAssignmentService.GetSubmissionResultAsync(userId, submissionId);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
