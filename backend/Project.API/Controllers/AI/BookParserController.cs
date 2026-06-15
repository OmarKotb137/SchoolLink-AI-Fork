using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.AI.Interfaces;
using Project.BLL.DTOs;
using Project.BLL.Interfaces;

namespace Project.API.Controllers.AI;

[Route("api/book-parser")]
[ApiController]
// [Authorize(Roles = "Admin,Teacher")]
public class BookParserController : ControllerBase
{
    private readonly IBookParserService _bookParserService;
    private readonly IUnitService _unitService;

    public BookParserController(
        IBookParserService bookParserService,
        IUnitService unitService)
    {
        _bookParserService = bookParserService;
        _unitService = unitService;
    }

    [HttpPost("preview")]
    [RequestSizeLimit(100_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 100_000_000)]
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

        if (result.IsSuccess && result.Data is not null)
        {
            return Ok(new
            {
                isSuccess = true,
                data = new
                {
                    previewId = result.Data.PreviewId,
                    units = result.Data.Units
                },
                statusCode = 200
            });
        }
        return BadRequest(result);
    }

    [HttpPost("save")]
    public async Task<IActionResult> Save(int subjectId, [FromQuery] int gradeLevelId, [FromBody] List<CreateUnitDto> units)
    {
        if (units is null || units.Count == 0)
            return BadRequest(new { message = "يجب إدخال وحدات على الأقل" });

        var result = await _bookParserService.SaveBookStructureAsync(subjectId, gradeLevelId, units);
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

    [HttpGet("subjects")]
    public async Task<IActionResult> GetParsedSubjects()
    {
        var result = await _unitService.GetParsedSubjectsWithStructureAsync();
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpGet("subjects/{subjectId}")]
    public async Task<IActionResult> GetSubjectStructure(int subjectId)
    {
        var result = await _unitService.GetUnitsWithLessonsBySubjectAsync(subjectId);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPut("units/{unitId}")]
    public async Task<IActionResult> UpdateUnit(int unitId, [FromBody] UpdateUnitRequest request)
    {
        var result = await _unitService.UpdateUnitAsync(unitId, request.Name, request.Content, request.PageStart, request.PageEnd);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPut("lessons/{lessonId}")]
    public async Task<IActionResult> UpdateLesson(int lessonId, [FromBody] UpdateLessonRequest request)
    {
        var result = await _unitService.UpdateLessonAsync(lessonId, request.Title, request.Content, request.PageStart, request.PageEnd);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("units/{unitId}/lessons")]
    public async Task<IActionResult> CreateLesson(int unitId, [FromBody] CreateLessonDto dto)
    {
        var result = await _unitService.CreateLessonAsync(unitId, dto);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("subjects/{subjectId}/units")]
    public async Task<IActionResult> CreateUnit(int subjectId, [FromBody] CreateUnitDto dto)
    {
        var result = await _unitService.CreateUnitAsync(subjectId, dto);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("lesson/re-extract")]
    public async Task<IActionResult> ReExtractLessonContent([FromBody] ReExtractLessonRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PreviewId) || string.IsNullOrWhiteSpace(request.LessonTitle))
            return BadRequest(new { message = "PreviewId وعنوان الدرس مطلوبان." });

        var result = await _bookParserService.ReExtractLessonContentAsync(
            request.PreviewId, request.LessonTitle, request.PageStart, request.PageEnd);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("unit/re-extract")]
    public async Task<IActionResult> ReExtractUnitContent([FromBody] ReExtractUnitRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PreviewId))
            return BadRequest(new { message = "PreviewId مطلوب." });

        var result = await _bookParserService.ReExtractUnitContentAsync(
            request.PreviewId, request.UnitName ?? "Unit", request.PageStart, request.PageEnd);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("units/{unitId}")]
    public async Task<IActionResult> DeleteUnit(int unitId)
    {
        var result = await _unitService.DeleteUnitAsync(unitId);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("lessons/{lessonId}")]
    public async Task<IActionResult> DeleteLesson(int lessonId)
    {
        var result = await _unitService.DeleteLessonAsync(lessonId);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}

public class GenerateLessonContentRequest
{
    public string Title { get; set; } = string.Empty;
    public string RawContent { get; set; } = string.Empty;
}

public class ReExtractLessonRequest
{
    public string PreviewId { get; set; } = string.Empty;
    public string LessonTitle { get; set; } = string.Empty;
    public int PageStart { get; set; }
    public int? PageEnd { get; set; }
}

public class ReExtractUnitRequest
{
    public string PreviewId { get; set; } = string.Empty;
    public string? UnitName { get; set; }
    public int PageStart { get; set; }
    public int? PageEnd { get; set; }
}

public class UpdateUnitRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Content { get; set; }
    public int? PageStart { get; set; }
    public int? PageEnd { get; set; }
}

public class UpdateLessonRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public int? PageStart { get; set; }
    public int? PageEnd { get; set; }
}
