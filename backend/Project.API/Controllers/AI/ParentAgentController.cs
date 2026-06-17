using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;
using Project.BLL.Interfaces;
using Project.Domain.Enums;

namespace Project.API.Controllers.AI;

[Route("api/ai/parent-agent")]
[ApiController]
[Authorize(Roles = "Parent")]
public class ParentAgentController : ControllerBase
{
    private readonly IParentAssistantAgent _agent;
    private readonly IAcademicYearService _academicYearService;
    private readonly ILogger<ParentAgentController> _logger;

    public ParentAgentController(
        IParentAssistantAgent agent,
        IAcademicYearService academicYearService,
        ILogger<ParentAgentController> logger)
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

    [HttpGet("progress/{studentId}")]
    public async Task<IActionResult> Progress(int studentId, CancellationToken ct)
    {
        var term = await ResolveCurrentTermAsync();
        var result = await _agent.GetProgressSummaryAsync(studentId, term, ct);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    [HttpGet("activities/{studentId}")]
    public async Task<IActionResult> Activities(int studentId, CancellationToken ct)
    {
        var term = await ResolveCurrentTermAsync();
        var result = await _agent.SuggestLearningActivitiesAsync(studentId, term, ct);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    [HttpGet("weak-areas/{studentId}")]
    public async Task<IActionResult> WeakAreas(int studentId, CancellationToken ct)
    {
        var term = await ResolveCurrentTermAsync();
        var result = await _agent.IdentifyWeakAreasAsync(studentId, term, ct);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    [HttpGet("resources")]
    public async Task<IActionResult> Resources(string subject, string gradeLevel, CancellationToken ct)
    {
        var term = await ResolveCurrentTermAsync();
        var result = await _agent.RecommendResourcesAsync(subject, gradeLevel, term, ct);
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
                UserRole = "Parent",
                CurrentTerm = term
            };

            var result = await _agent.ChatAsync(request.Message, request.ConversationId, context, ct);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ParentAgent chat failed");
            return StatusCode(500, new { error = "حدث خطأ داخلي. يرجى المحاولة مرة أخرى." });
        }
    }
}
