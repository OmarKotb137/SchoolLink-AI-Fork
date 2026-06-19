using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[ApiController]
[Route("api/parent-dashboard")]
[Authorize(Roles = "Parent")]
public class ParentDashboardController : ControllerBase
{
    private readonly IParentStudentService _parentStudentService;
    private readonly IParentDashboardService _parentDashboardService;

    public ParentDashboardController(
        IParentStudentService parentStudentService,
        IParentDashboardService parentDashboardService)
    {
        _parentStudentService = parentStudentService;
        _parentDashboardService = parentDashboardService;
    }

    [HttpGet("my-children")]
    public async Task<IActionResult> GetMyChildren()
    {
        var parentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _parentStudentService.GetDashboardChildrenByParentAsync(parentId);
        if (!result.IsSuccess)
            return NotFound(result);

        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard([FromQuery] int? term = null)
    {
        var parentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _parentDashboardService.GetParentDashboardAsync(parentId, term);
        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }
}
