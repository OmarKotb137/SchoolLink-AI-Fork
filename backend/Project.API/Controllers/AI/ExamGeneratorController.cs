using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.AI.Interfaces;
using Project.BLL.DTOs.Exam;
using Project.BLL.Interfaces;

namespace Project.API.Controllers.AI;

[Route("api/ai/exam-generator")]
[ApiController]
[Authorize(Roles = "Admin,Teacher")]
public class ExamGeneratorController : ControllerBase
{
    private readonly IExamService _examService;
    private readonly IAiExamGeneratorService _aiGen;

    public ExamGeneratorController(IExamService examService, IAiExamGeneratorService aiGen)
    {
        _examService = examService;
        _aiGen = aiGen;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] CreateExamFromAiDto dto, CancellationToken ct)
    {
        var result = await _examService.CreateFromAiAsync(dto, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("ai-generate")]
    public async Task<IActionResult> AiGenerate([FromBody] AiGenerateExamRequest request, CancellationToken ct)
    {
        var result = await _aiGen.GenerateExamAsync(request, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("preview")]
    public async Task<IActionResult> Preview([FromBody] AiGenerateExamRequest request, CancellationToken ct)
    {
        var result = await _aiGen.PreviewExamAsync(request, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody] CreateExamFromAiDto dto, CancellationToken ct)
    {
        var result = await _aiGen.SaveGeneratedExamAsync(dto, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpGet("history")]
    public async Task<IActionResult> History()
    {
        var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _examService.GetAiExamHistoryByTeacherAsync(teacherId);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
