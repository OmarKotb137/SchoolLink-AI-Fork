using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.Interfaces;

namespace Project.API.Controllers.AI;

[Route("api/ai/tts")]
[ApiController]
[Authorize(Roles = "Student")]
public class TextToSpeechController : ControllerBase
{
    private readonly ITextToSpeechService _ttsService;
    private readonly ILogger<TextToSpeechController> _logger;

    public TextToSpeechController(ITextToSpeechService ttsService, ILogger<TextToSpeechController> logger)
    {
        _ttsService = ttsService;
        _logger = logger;
    }

    /// <summary>
    /// تحويل النص إلى صوت باستخدام Gemini TTS
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Synthesize([FromBody] TtsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Text))
            return BadRequest(new { isSuccess = false, message = "النص مطلوب للتحويل إلى صوت" });

        if (request.Text.Length > 2000)
            return BadRequest(new { isSuccess = false, message = "النص طويل جداً، الحد الأقصى 2000 حرف" });

        _logger.LogInformation("TTS request for text of length {Length} chars.", request.Text.Length);

        var audioData = await _ttsService.SynthesizeSpeechAsync(request.Text);

        if (audioData == null || audioData.Length == 0)
            return StatusCode(500, new { isSuccess = false, message = "فشل تحويل النص إلى صوت" });

        return File(audioData, "audio/wav", "speech.wav");
    }

    public class TtsRequest
    {
        public string Text { get; set; } = string.Empty;
    }
}
