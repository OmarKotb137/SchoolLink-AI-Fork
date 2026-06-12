using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.API.Services;

namespace Project.API.Controllers.AI;

[Route("api/ai/transcribe")]
[ApiController]
[Authorize]
public class VoiceTranscriptionController : ControllerBase
{
    private readonly WhisperTranscriptionService _whisper;
    private readonly ILogger<VoiceTranscriptionController> _logger;

    public VoiceTranscriptionController(WhisperTranscriptionService whisper, ILogger<VoiceTranscriptionController> logger)
    {
        _whisper = whisper;
        _logger = logger;
    }

    /// <summary>
    /// استقبال ملف صوتي واستخراج النص منه باستخدام Whisper
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(25 * 1024 * 1024)] // 25MB max
    public async Task<IActionResult> Transcribe(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { isSuccess = false, message = "الملف الصوتي مطلوب" });

        var supportedTypes = new[] { ".mp3", ".wav", ".m4a", ".ogg", ".webm" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!supportedTypes.Contains(ext))
            return BadRequest(new { isSuccess = false, message = "نوع الملف غير مدعوم. الأنواع المدعومة: mp3, wav, m4a, ogg, webm" });

        var tempPath = Path.Combine(Path.GetTempPath(), $"voice_{Guid.NewGuid():N}{ext}");

        try
        {
            await using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation("Transcribing audio file: {Name}, size: {Size} bytes", file.FileName, file.Length);

            var text = await _whisper.TranscribeAsync(tempPath);

            if (string.IsNullOrWhiteSpace(text))
                return Ok(new { isSuccess = true, text = "", message = "لم يتم التعرف على كلام" });

            return Ok(new { isSuccess = true, text });
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Whisper model not found");
            return StatusCode(503, new { isSuccess = false, message = "خدمة التعرف على الصوت غير متاحة حالياً" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transcription failed for file {Name}", file.FileName);
            return StatusCode(500, new { isSuccess = false, message = "حدث خطأ أثناء معالجة الملف الصوتي" });
        }
        finally
        {
            if (System.IO.File.Exists(tempPath))
                System.IO.File.Delete(tempPath);
        }
    }
}
