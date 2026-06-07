using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.Assignment;
using Project.BLL.DTOs.AssignmentQuestion;
using Project.BLL.Interfaces;
using Project.Domain.Enums;

namespace Project.API.Controllers;

[ApiController]
[Route("api/assignments")]
[Authorize]
public class AssignmentsController : ControllerBase
{
    private readonly IAssignmentService _service;

    public AssignmentsController(IAssignmentService service)
    {
        _service = service;
    }

    [HttpGet("by-class-subject-teacher/{classSubjectTeacherId:int}")]
    public async Task<IActionResult> GetAllByClassSubjectTeacher(int classSubjectTeacherId)
    {
        var result = await _service.GetAllByClassSubjectTeacherAsync(classSubjectTeacherId);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,Teacher")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAssignmentDto dto)
    {
        var result = await _service.CreateAsync(dto);
        if (!result.IsSuccess)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    [Authorize(Roles = "Admin,Teacher")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAssignmentDto dto)
    {
        if (id != dto.Id)
            return BadRequest("معرف الواجب في الرابط لا يطابق المعرف في الطلب");
        var result = await _service.UpdateAsync(dto);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,Teacher")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(id);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,Teacher")]
    [HttpPatch("{id:int}/publish")]
    public async Task<IActionResult> Publish(int id)
    {
        var result = await _service.PublishAsync(id);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,Teacher")]
    [HttpPatch("{id:int}/unpublish")]
    public async Task<IActionResult> UnPublish(int id)
    {
        var result = await _service.UnPublishAsync(id);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,Teacher")]
    [HttpPost("questions")]
    public async Task<IActionResult> AddQuestion([FromBody] CreateAssignmentQuestionDto dto)
    {
        var result = await _service.AddQuestionAsync(dto);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,Teacher")]
    [HttpPut("questions/{questionId:int}")]
    public async Task<IActionResult> UpdateQuestion(int questionId, [FromBody] UpdateAssignmentQuestionDto dto)
    {
        if (questionId != dto.Id)
            return BadRequest("معرف السؤال في الرابط لا يطابق المعرف في الطلب");
        var result = await _service.UpdateQuestionAsync(dto);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,Teacher")]
    [HttpDelete("questions/{questionId:int}")]
    public async Task<IActionResult> DeleteQuestion(int questionId)
    {
        var result = await _service.DeleteQuestionAsync(questionId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("by-teacher/{teacherId:int}")]
    public async Task<IActionResult> GetByTeacher(int teacherId, [FromQuery] int academicYearId)
    {
        var result = await _service.GetByTeacherAsync(teacherId, academicYearId);
        return Ok(result);
    }

    [HttpGet("by-class-subject-teacher/{classSubjectTeacherId:int}/summary")]
    public async Task<IActionResult> GetSummary(int classSubjectTeacherId, [FromQuery] EvaluationCategory? category = null)
    {
        var result = await _service.GetAssignmentsByClassSubjectTeacherAsync(classSubjectTeacherId, category);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,Teacher")]
    [HttpPost("generate-with-ai")]
    public async Task<IActionResult> GenerateWithAI([FromBody] GenerateAssignmentRequest request)
    {
        var result = await _service.GenerateAssignmentWithAIAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }
}