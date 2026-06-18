using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.StudentExams;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[Authorize(Roles = "Student")]
[ApiController]
[Route("api/student/exams")]
public class StudentExamsController : ControllerBase
{
    private readonly IStudentExamService _studentExamService;

    public StudentExamsController(IStudentExamService studentExamService)
    {
        _studentExamService = studentExamService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyExams()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _studentExamService.GetMyExamsAsync(userId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("{examId:int}")]
    public async Task<IActionResult> GetExamDetails(int examId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _studentExamService.GetMyExamDetailsAsync(userId, examId);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost("{examId:int}/start")]
    public async Task<IActionResult> Start(int examId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _studentExamService.StartOrResumeAttemptAsync(userId, examId);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpGet("{examId:int}/active-attempt")]
    public async Task<IActionResult> GetActiveAttempt(int examId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _studentExamService.GetActiveAttemptAsync(userId, examId);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost("/api/student/exam-attempts/{attemptId:int}/submit")]
    public async Task<IActionResult> Submit(int attemptId, [FromBody] SubmitStudentExamAttemptDto dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _studentExamService.SubmitAttemptAsync(userId, attemptId, dto);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPatch("/api/student/exam-attempts/{attemptId:int}/answers")]
    public async Task<IActionResult> SaveAnswerProgress(int attemptId, [FromBody] SaveAnswerProgressDto dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _studentExamService.SaveAnswerProgressAsync(userId, attemptId, dto);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpGet("/api/student/exam-attempts/{attemptId:int}/result")]
    public async Task<IActionResult> GetResult(int attemptId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _studentExamService.GetAttemptResultAsync(userId, attemptId);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
