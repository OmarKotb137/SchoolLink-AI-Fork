using Microsoft.AspNetCore.Mvc;
using Project.BLL.AI.ExamAgent.Models;
using Project.BLL.AI.ExamAgent.Services;
using Project.BLL.AI.Interfaces;

namespace Project.API.Controllers.AI;

[ApiController]
[Route("api/ai/exam-agent")]
public class ExamAgentChatController : ControllerBase
{
    private readonly ExamAgentService _agent;
    private readonly IAgentChatStore _chatStore;
    private readonly ILogger<ExamAgentChatController> _logger;

    public ExamAgentChatController(ExamAgentService agent, IAgentChatStore chatStore, ILogger<ExamAgentChatController> logger)
    {
        _agent = agent;
        _chatStore = chatStore;
        _logger = logger;
    }

    [HttpPost("run")]
    public async Task<IActionResult> Run([FromBody] AgentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserMessage))
            return BadRequest(new { error = "UserMessage is required." });

        try
        {
            var result = await _agent.RunAsync(request.UserMessage);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExamAgent error");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "Message is required." });

        try
        {
            var conversationId = request.ConversationId ?? Guid.NewGuid().ToString();
            var result = await _agent.RunAsync(request.Message, conversationId);
            return Ok(new { response = result.Answer, conversationId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExamAgent chat error");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("save-message")]
    public async Task<IActionResult> SaveMessage([FromBody] SaveMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ConversationId) ||
            string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { error = "ConversationId and Content are required." });

        try
        {
            await _chatStore.SaveMessageAsync(request.ConversationId, request.Sender, request.Content, "exam");
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving message");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("conversation/{conversationId}")]
    public async Task<IActionResult> GetConversation(string conversationId)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
            return BadRequest(new { error = "ConversationId is required." });

        try
        {
            var messages = await _chatStore.GetRecentMessagesAsync(conversationId, 100);
            return Ok(new { messages });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conversation");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
