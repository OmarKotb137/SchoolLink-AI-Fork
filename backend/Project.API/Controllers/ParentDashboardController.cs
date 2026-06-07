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

    public ParentDashboardController(IParentStudentService parentStudentService)
    {
        _parentStudentService = parentStudentService;
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
}
