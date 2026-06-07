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
    private readonly IParentDashboardService _parentDashboardService;

    public ParentDashboardController(IParentDashboardService parentDashboardService)
    {
        _parentDashboardService = parentDashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var parentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _parentDashboardService.GetParentDashboardAsync(parentId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }
}
