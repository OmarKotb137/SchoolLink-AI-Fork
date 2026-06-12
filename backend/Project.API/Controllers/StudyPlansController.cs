using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.StudyPlans;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[ApiController]
[Route("api/study-plans")]
[Authorize(Roles = "Admin,Teacher,Student,Parent")]
public class StudyPlansController : ControllerBase
{
    private readonly IStudyPlanService _studyPlanService;

    public StudyPlansController(IStudyPlanService studyPlanService)
    {
        _studyPlanService = studyPlanService;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateWithAI([FromBody] GenerateStudyPlanRequest request)
    {
        var result = await _studyPlanService.GenerateStudyPlanWithAIAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("manual")]
    public async Task<IActionResult> CreateManual([FromBody] CreateStudyPlanRequest request)
    {
        var result = await _studyPlanService.CreateManualStudyPlanAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("active/{enrollmentId:int}")]
    public async Task<IActionResult> GetActive(int enrollmentId)
    {
        var result = await _studyPlanService.GetActiveStudyPlanAsync(enrollmentId);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("{enrollmentId:int}")]
    public async Task<IActionResult> GetAll(int enrollmentId)
    {
        var result = await _studyPlanService.GetAllStudyPlansAsync(enrollmentId);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpPatch("sessions/{itemId:int}/complete")]
    public async Task<IActionResult> MarkComplete(int itemId, [FromQuery] int enrollmentId)
    {
        var result = await _studyPlanService.MarkSessionCompleteAsync(itemId, enrollmentId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPatch("sessions/{itemId:int}/incomplete")]
    public async Task<IActionResult> MarkIncomplete(int itemId, [FromQuery] int enrollmentId)
    {
        var result = await _studyPlanService.MarkSessionIncompleteAsync(itemId, enrollmentId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("sessions/{itemId:int}")]
    public async Task<IActionResult> UpdateSession(int itemId, [FromBody] UpdateStudyPlanItemRequest request)
    {
        if (itemId != request.Id)
            return BadRequest("معرف الجلسة في الرابط لا يطابق المعرف في الطلب");

        var result = await _studyPlanService.UpdateStudyPlanItemAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("sessions/{itemId:int}")]
    public async Task<IActionResult> DeleteSession(int itemId, [FromQuery] int enrollmentId)
    {
        var result = await _studyPlanService.DeleteSessionAsync(itemId, enrollmentId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPatch("{planId:int}/rest-day")]
    public async Task<IActionResult> UpdateRestDay(int planId, [FromBody] UpdateRestDayRequest request)
    {
        var result = await _studyPlanService.UpdateRestDayAsync(planId, request.RestDay);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var result = await _studyPlanService.DeactivateStudyPlanAsync(id);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }
}
