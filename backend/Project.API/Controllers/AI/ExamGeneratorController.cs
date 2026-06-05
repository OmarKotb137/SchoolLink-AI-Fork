using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.Exam;
using Project.BLL.Interfaces;

namespace Project.API.Controllers.AI;

[Route("api/ai/exam-generator")]
[ApiController]
[Authorize(Roles = "Admin,Teacher")]
public class ExamGeneratorController : ControllerBase
{
    private readonly IExamService _examService;

    public ExamGeneratorController(IExamService examService) => _examService = examService;

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] CreateExamFromAiDto dto, CancellationToken ct)
    {
        var result = await _examService.CreateFromAiAsync(dto, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
