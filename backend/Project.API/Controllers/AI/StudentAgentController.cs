using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;
using Project.BLL.Interfaces;
using Project.Domain.Enums;

namespace Project.API.Controllers.AI;

[Route("api/ai/student-agent")]
[ApiController]
[Authorize(Roles = "Student")]
public class StudentAgentController : ControllerBase
{
    private readonly IStudentAssistantAgent _agent;
    private readonly IAcademicYearService _academicYearService;
    private readonly ILogger<StudentAgentController> _logger;

    public StudentAgentController(
        IStudentAssistantAgent agent,
        IAcademicYearService academicYearService,
        ILogger<StudentAgentController> logger)
    {
        _agent = agent;
        _academicYearService = academicYearService;
        _logger = logger;
    }

    private async Task<AcademicTerm?> ResolveCurrentTermAsync()
    {
        var result = await _academicYearService.GetCurrentTermAsync();
        return result.IsSuccess ? result.Data : null;
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AiQuestionRequest request, CancellationToken ct)
    {
        var term = await ResolveCurrentTermAsync();
        var result = await _agent.AnswerQuestionAsync(request, term, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("explain")]
    public async Task<IActionResult> Explain([FromQuery] string concept, [FromQuery] string subject, [FromQuery] string gradeLevel, CancellationToken ct)
    {
        var term = await ResolveCurrentTermAsync();
        var result = await _agent.ExplainConceptAsync(concept, subject, gradeLevel, term, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("practice")]
    public async Task<IActionResult> Practice([FromQuery] string subject, [FromQuery] string topic, CancellationToken ct, [FromQuery] int count = 5)
    {
        var term = await ResolveCurrentTermAsync();
        var result = await _agent.GeneratePracticeExerciseAsync(subject, topic, count, term, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("analyze-answer")]
    public async Task<IActionResult> AnalyzeAnswer([FromQuery] string questionText, [FromQuery] string studentAnswer, CancellationToken ct, [FromQuery] string? modelAnswer = null)
    {
        var term = await ResolveCurrentTermAsync();
        var result = await _agent.AnalyzeAnswerAsync(questionText, studentAnswer, modelAnswer, term, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] AgentChatRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "Message is required." });

        try
        {
            var term = await ResolveCurrentTermAsync();
            var context = new UserContext
            {
                UserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value),
                UserRole = "Student",
                CurrentTerm = term
            };

            var result = await _agent.ChatAsync(request.Message, request.ConversationId, context, ct);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StudentAgent chat failed");
            return StatusCode(500, new { error = "حدث خطأ داخلي. يرجى المحاولة مرة أخرى." });
        }
    }
}
