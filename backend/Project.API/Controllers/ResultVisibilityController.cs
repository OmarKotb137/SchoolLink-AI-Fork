using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.ResultVisibility;
using Project.BLL.Interfaces;
using Project.Domain.Enums;

namespace Project.API.Controllers;

[Authorize(Roles = "Admin,Teacher")]
[ApiController]
[Route("api/[controller]")]
public class ResultVisibilityController : ControllerBase
{
    private readonly IResultVisibilityService _resultVisibilityService;

    public ResultVisibilityController(IResultVisibilityService resultVisibilityService)
    {
        _resultVisibilityService = resultVisibilityService;
    }

    [HttpPost]
    public async Task<IActionResult> SetVisibility([FromBody] SetVisibilityRequest request)
    {
        request.ControlledById = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _resultVisibilityService.SetVisibilityAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("check")]
    public async Task<IActionResult> CheckVisibility([FromQuery] int academicYearId, [FromQuery] AcademicTerm term)
    {
        var result = await _resultVisibilityService.IsResultsVisibleAsync(academicYearId, term);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _resultVisibilityService.GetAllSettingsAsync();
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("academic-year/{academicYearId}")]
    public async Task<IActionResult> GetByAcademicYear(int academicYearId)
    {
        var result = await _resultVisibilityService.GetSettingsByAcademicYearAsync(academicYearId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateVisibilityRequest request)
    {
        var result = await _resultVisibilityService.UpdateVisibilitySettingAsync(id, request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _resultVisibilityService.DeleteVisibilitySettingAsync(id);
        if (!result.IsSuccess)
            return BadRequest(result);
        return NoContent();
    }
}
