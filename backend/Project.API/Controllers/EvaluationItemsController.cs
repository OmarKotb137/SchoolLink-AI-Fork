using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.EvaluationItems;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EvaluationItemsController : ControllerBase
{
    private readonly IEvaluationItemService _service;

    public EvaluationItemsController(IEvaluationItemService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEvaluationItemRequest request)
    {
        var result = await _service.CreateEvaluationItemAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetByTemplate), new { templateId = request.TemplateId }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEvaluationItemRequest request)
    {
        if (id != request.Id)
            return BadRequest("معرف البند في الرابط لا يطابق المعرف في الطلب");
        var result = await _service.UpdateEvaluationItemAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteEvaluationItemAsync(id);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPatch("{id:int}/toggle-visibility")]
    public async Task<IActionResult> ToggleVisibility(int id)
    {
        var result = await _service.ToggleItemVisibilityAsync(id);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("by-template/{templateId:int}")]
    public async Task<IActionResult> GetByTemplate(int templateId)
    {
        var result = await _service.GetItemsByTemplateAsync(templateId);
        return Ok(result);
    }
}
