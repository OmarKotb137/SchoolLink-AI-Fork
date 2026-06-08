using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.AIGenerationLog;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[ApiController]
[Route("api/ai-generation-logs")]
[Authorize(Roles = "Admin")]
public class AIGenerationLogsController : ControllerBase
{
    private readonly IAIGenerationLogService _service;

    public AIGenerationLogsController(IAIGenerationLogService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
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

    [HttpGet("by-user/{userId:int}")]
    public async Task<IActionResult> GetByUser(int userId)
    {
        var result = await _service.GetByUserIdAsync(userId);
        return Ok(result);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var result = await _service.GetSummaryAsync();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAIGenerationLogDto dto)
    {
        var result = await _service.CreateAsync(dto);
        if (!result.IsSuccess)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(id);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("older-than")]
    public async Task<IActionResult> DeleteOlderThan([FromQuery] DateTime cutoff)
    {
        var result = await _service.DeleteOlderThanAsync(cutoff);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }
}