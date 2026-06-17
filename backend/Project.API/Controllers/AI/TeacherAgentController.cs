using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;
using Project.BLL.Interfaces;
using Project.Domain.Enums;

namespace Project.API.Controllers.AI;

[Route("api/ai/teacher-agent")]
[ApiController]
[Authorize(Roles = "Admin,Teacher")]
public class TeacherAgentController : ControllerBase
{
    private readonly ITeacherAssistantAgent _agent;
    private readonly IAcademicYearService _academicYearService;
    private readonly ILogger<TeacherAgentController> _logger;

    public TeacherAgentController(
        ITeacherAssistantAgent agent,
        IAcademicYearService academicYearService,
        ILogger<TeacherAgentController> logger)
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

    [HttpPost("lesson-plan")]
    public async Task<IActionResult> LessonPlan([FromBody] LessonPlanRequest request, CancellationToken ct)
    {
        var result = await _agent.SuggestLessonPlanAsync(request, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("generate-quiz")]
    public async Task<IActionResult> GenerateQuiz([FromQuery] string subject, [FromQuery] string topic, CancellationToken ct, [FromQuery] int count = 10)
    {
        var result = await _agent.GenerateQuizAsync(subject, topic, count, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("grade")]
    public async Task<IActionResult> Grade([FromQuery] string questionText, [FromQuery] string modelAnswer, [FromQuery] string studentAnswer, CancellationToken ct)
    {
        var result = await _agent.GradeSubmissionAsync(questionText, modelAnswer, studentAnswer, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("resources")]
    public async Task<IActionResult> Resources([FromQuery] string subject, [FromQuery] string topic, CancellationToken ct)
    {
        var result = await _agent.SuggestTeachingResourcesAsync(subject, topic, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("analyze-class")]
    public async Task<IActionResult> AnalyzeClass([FromBody] List<Dictionary<string, object>> scores, CancellationToken ct)
    {
        var result = await _agent.AnalyzeClassPerformanceAsync(scores, ct);
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
                UserRole = "Teacher",
                CurrentTerm = term
            };

            var result = await _agent.ChatAsync(request.Message, request.ConversationId, context, ct);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TeacherAgent chat failed");
            return StatusCode(500, new { error = "حدث خطأ داخلي. يرجى المحاولة مرة أخرى." });
        }
    }
}
