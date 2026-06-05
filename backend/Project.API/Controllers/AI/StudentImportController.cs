using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.AI.Interfaces;

namespace Project.API.Controllers.AI;

[Route("api/ai/student-import")]
[ApiController]
[Authorize(Roles = "Admin")]
public class StudentImportController : ControllerBase
{
    private readonly IStudentImportService _service;

    public StudentImportController(IStudentImportService service) => _service = service;

    [HttpPost("preview")]
    public async Task<IActionResult> Preview(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest("الملف مطلوب");

        using var stream = file.OpenReadStream();
        var result = await _service.PreviewImportAsync(stream, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("import")]
    public async Task<IActionResult> Import(IFormFile file, int classId, int academicYearId, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest("الملف مطلوب");

        using var stream = file.OpenReadStream();
        var result = await _service.ImportFromExcelAsync(stream, classId, academicYearId, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
