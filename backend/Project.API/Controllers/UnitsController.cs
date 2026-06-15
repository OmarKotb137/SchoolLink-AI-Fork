using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Teacher")]
public class UnitsController : ControllerBase
{
    private readonly IUnitService _unitService;

    public UnitsController(IUnitService unitService)
    {
        _unitService = unitService;
    }

    [HttpGet("api/subjects/{subjectId}/units-with-lessons")]
    public async Task<IActionResult> GetUnitsWithLessons(int subjectId, [FromQuery] int? gradeLevelId = null)
    {
        if (gradeLevelId.HasValue && gradeLevelId > 0)
        {
            var result = await _unitService.GetUnitsByGradeLevelAndSubjectAsync(gradeLevelId.Value, subjectId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
        var resultAll = await _unitService.GetUnitsWithLessonsBySubjectAsync(subjectId);
        return resultAll.IsSuccess ? Ok(resultAll) : BadRequest(resultAll);
    }

    [HttpGet("api/subjects/{subjectId}/units")]
    public async Task<IActionResult> GetBySubject(int subjectId, [FromQuery] int? gradeLevelId = null)
    {
        if (gradeLevelId.HasValue && gradeLevelId > 0)
        {
            var result = await _unitService.GetUnitsByGradeLevelAndSubjectAsync(gradeLevelId.Value, subjectId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
        var resultAll = await _unitService.GetUnitsBySubjectAsync(subjectId);
        return resultAll.IsSuccess ? Ok(resultAll) : BadRequest(resultAll);
    }

    [HttpGet("api/units/{unitId}/lessons")]
    public async Task<IActionResult> GetLessons(int unitId)
    {
        var result = await _unitService.GetLessonsByUnitAsync(unitId);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("api/subjects/{subjectId}/units")]
    public async Task<IActionResult> Create(int subjectId, [FromBody] CreateUnitDto dto)
    {
        var result = await _unitService.CreateUnitAsync(subjectId, dto.Name, dto.DisplayOrder, dto.Lessons);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("api/units/{unitId}")]
    public async Task<IActionResult> Delete(int unitId)
    {
        var result = await _unitService.DeleteUnitAsync(unitId);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}