using Microsoft.AspNetCore.Mvc;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;

namespace Project.API.Controllers.AI;

[ApiController]
[Route("api/ai/test")]
public class AiTestController : ControllerBase
{
    private readonly ILLMRouter _router;
    private readonly IEnumerable<ILLMProvider> _providers;

    public AiTestController(ILLMRouter router, IEnumerable<ILLMProvider> providers)
    {
        _router = router;
        _providers = providers;
    }

    [HttpGet("providers")]
    public IActionResult GetProviders()
    {
        var list = _providers.Select(p => new { name = p.ProviderName });
        return Ok(list);
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromQuery] string provider, [FromBody] TestChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "Message is required." });

        try
        {
            var messages = new List<ChatMessage>
            {
                new() { Role = "user", Content = request.Message }
            };
            var result = await _router.GenerateChatAsync(
                "أنت مساعد ذكي. أجب بإجابة مختصرة مفيدة.",
                messages,
                provider);
            return Ok(new { provider, response = result });
        }
        catch (Exception ex)
        {
            return Ok(new { provider, error = ex.Message });
        }
    }
}

public class TestChatRequest
{
    public string Message { get; set; } = string.Empty;
}
