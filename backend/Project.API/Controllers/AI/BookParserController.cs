using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.AI.Interfaces;
using Project.BLL.DTOs;

namespace Project.API.Controllers.AI;

[Route("api/book-parser")]
[ApiController]
// [Authorize(Roles = "Admin,Teacher")]
public class BookParserController : ControllerBase
{
    private readonly IBookParserService _bookParserService;

    public BookParserController(IBookParserService bookParserService)
    {
        _bookParserService = bookParserService;
    }

    [HttpPost("preview")]
    public async Task<IActionResult> Preview(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "الملف مطلوب" });

        if (Path.GetExtension(file.FileName).ToLower() != ".pdf")
            return BadRequest(new { message = "يرجى رفع ملف PDF فقط" });

        if (file.Length > 100 * 1024 * 1024)
            return BadRequest(new { message = "حجم الملف يتجاوز 100 ميجابايت" });

        using var stream = file.OpenReadStream();
        var result = await _bookParserService.PreviewBookAsync(stream, file.FileName);

        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("save")]
    public async Task<IActionResult> Save(int subjectId, [FromBody] List<CreateUnitDto> units)
    {
        if (units is null || units.Count == 0)
            return BadRequest(new { message = "يجب إدخال وحدات على الأقل" });

        var result = await _bookParserService.SaveBookStructureAsync(subjectId, units);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("lesson/generate-content")]
    public async Task<IActionResult> GenerateLessonContent([FromBody] GenerateLessonContentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RawContent) || string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { message = "النص الخام وعنوان الدرس مطلوبان." });

        var result = await _bookParserService.CleanLessonContentWithAiAsync(request.RawContent, request.Title);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}

public class GenerateLessonContentRequest
{
    public string Title { get; set; } = string.Empty;
    public string RawContent { get; set; } = string.Empty;
}
