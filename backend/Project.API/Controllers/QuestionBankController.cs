using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.QuestionBank;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,Teacher")]
public class QuestionBankController : ControllerBase
{
    private readonly IQuestionBankService _questionBankService;

    public QuestionBankController(IQuestionBankService questionBankService)
    {
        _questionBankService = questionBankService;
    }

    [HttpGet("subject/{subjectId}")]
    public async Task<IActionResult> GetBySubject(int subjectId)
    {
        var result = await _questionBankService.GetBySubjectAsync(subjectId);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _questionBankService.GetByIdAsync(id);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] SearchQuestionBankDto dto)
    {
        var result = await _questionBankService.SearchAsync(dto);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> AddQuestion([FromBody] AddToQuestionBankDto dto)
    {
        var result = await _questionBankService.AddQuestionAsync(dto);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("bulk-from-exam/{examId}")]
    public async Task<IActionResult> BulkAddFromExam(int examId, [FromQuery] int subjectId)
    {
        var result = await _questionBankService.BulkAddFromExamAsync(examId, subjectId);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _questionBankService.DeleteAsync(id);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }
}
