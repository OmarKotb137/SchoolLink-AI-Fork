using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[ApiController]
[Route("api/grade-levels")]
[Authorize]
public class GradeLevelController : ControllerBase
{
    private readonly IGradeLevelService _gradeLevelService;

    public GradeLevelController(IGradeLevelService gradeLevelService)
    {
        _gradeLevelService = gradeLevelService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _gradeLevelService.GetAllGradeLevelsAsync();
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _gradeLevelService.GetGradeLevelByIdAsync(id);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,Teacher")]
    [HttpGet("{id:int}/with-classes")]
    public async Task<IActionResult> GetWithClasses(int id)
    {
        var result = await _gradeLevelService.GetGradeLevelWithClassesAsync(id);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGradeLevelRequest request)
    {
        var result = await _gradeLevelService.CreateGradeLevelAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("validated")]
    public async Task<IActionResult> CreateValidated([FromBody] CreateGradeLevelRequest request)
    {
        var result = await _gradeLevelService.CreateGradeLevelValidatedAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateGradeLevelRequest request)
    {
        if (id != request.Id)
            return BadRequest("معرّف الرابط لا يطابق معرّف الطلب.");

        var result = await _gradeLevelService.UpdateGradeLevelAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}/validated")]
    public async Task<IActionResult> UpdateValidated(int id, [FromBody] UpdateGradeLevelRequest request)
    {
        if (id != request.Id)
            return BadRequest("معرّف الرابط لا يطابق معرّف الطلب.");

        var result = await _gradeLevelService.UpdateGradeLevelValidatedAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _gradeLevelService.DeleteGradeLevelAsync(id);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }
}
