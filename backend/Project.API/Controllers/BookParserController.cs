using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[Route("api/book-parser")]
[ApiController]
[Authorize(Roles = "Admin,Teacher")]
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
}