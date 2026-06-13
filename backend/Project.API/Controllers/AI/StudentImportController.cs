using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;

namespace Project.API.Controllers.AI;

[Route("api/ai/student-import")]
[ApiController]
[Authorize(Roles = "Admin")]
public class StudentImportController : ControllerBase
{
    private readonly IStudentImportService _service;

    public StudentImportController(IStudentImportService service) => _service = service;

    [HttpPost("preview")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<IActionResult> Preview(List<IFormFile> files, CancellationToken ct)
    {
        if (files == null || files.Count == 0)
            return BadRequest(new { error = "يرجى رفع ملف واحد على الأقل" });

        var fileDataList = new List<FileData>();
        foreach (var f in files)
        {
            using var ms = new MemoryStream();
            await f.CopyToAsync(ms, ct);
            fileDataList.Add(new FileData
            {
                Data = ms.ToArray(),
                FileName = f.FileName,
                ContentType = f.ContentType
            });
        }

        var result = await _service.PreviewImportAsync(fileDataList, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("import")]
    public async Task<IActionResult> Import([FromBody] AiImportRequest request, CancellationToken ct)
    {
        if (request.Students == null || request.Students.Count == 0)
            return BadRequest(new { error = "يجب توفير طالب واحد على الأقل" });

        var result = await _service.ImportWithAiAsync(request.Students, request.ClassId, request.AcademicYearId, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}

public class AiImportRequest
{
    public List<ImportedStudentDto> Students { get; set; } = new();
    public int? ClassId { get; set; }
    public int? AcademicYearId { get; set; }
}
