using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.EvaluationTemplates;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Teacher")]
public class EvaluationTemplatesController : ControllerBase
{
    private readonly IEvaluationTemplateService _service;

    public EvaluationTemplatesController(IEvaluationTemplateService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEvaluationTemplateRequest request)
    {
        var result = await _service.CreateEvaluationTemplateAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEvaluationTemplateRequest request)
    {
        if (id != request.Id)
            return BadRequest("معرف القالب في الرابط لا يطابق المعرف في الطلب");
        var result = await _service.UpdateEvaluationTemplateAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteEvaluationTemplateAsync(id);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetTemplateByIdAsync(id);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("by-grade-level")]
    public async Task<IActionResult> GetByGradeLevel(
        [FromQuery] int gradeLevelId,
        [FromQuery] int academicYearId)
    {
        var result = await _service.GetTemplateByGradeLevelAsync(gradeLevelId, academicYearId);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllTemplatesAsync();
        return Ok(result);
    }

    [HttpGet("by-subject/{subjectId:int}")]
    public async Task<IActionResult> GetBySubject(int subjectId, [FromQuery] int academicYearId)
    {
        var result = await _service.GetTemplatesBySubjectAsync(subjectId, academicYearId);
        return Ok(result);
    }

    [HttpPatch("{id:int}/toggle-active")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var result = await _service.ToggleTemplateActiveAsync(id);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("{id:int}/duplicate")]
    public async Task<IActionResult> Duplicate(int id)
    {
        var result = await _service.DuplicateTemplateAsync(id);
        if (!result.IsSuccess)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }
}
