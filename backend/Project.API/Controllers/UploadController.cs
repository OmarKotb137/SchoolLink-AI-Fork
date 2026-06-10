using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Project.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<UploadController> _logger;

    public UploadController(IWebHostEnvironment env, ILogger<UploadController> logger)
    {
        _env = env;
        _logger = logger;
    }

    [HttpPost("chat")]
    public async Task<IActionResult> UploadChatFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { isSuccess = false, message = "الملف غير صالح" });

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".mp4", ".mov", ".avi" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
            return BadRequest(new { isSuccess = false, message = "نوع الملف غير مدعوم" });

        if (file.Length > 50 * 1024 * 1024)
            return BadRequest(new { isSuccess = false, message = "حجم الملف يجب أن لا يتجاوز 50 ميغابايت" });

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "chat");
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        var url = $"/uploads/chat/{fileName}";
        _logger.LogInformation("File uploaded: {Url}", url);

        return Ok(new { isSuccess = true, data = new { url, fileName = file.FileName, ext } });
    }
}
