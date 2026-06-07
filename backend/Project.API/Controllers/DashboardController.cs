using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAdminDashboard([FromQuery] int? academicYearId = null)
    {
        var result = await _dashboardService.GetAdminDashboardAsync(academicYearId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }
}
