using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[Authorize(Roles = "Teacher")]
[ApiController]
[Route("api/teacher-dashboard")]
public class TeacherDashboardController : ControllerBase
{
    private readonly ITeacherDashboardService _teacherDashboardService;

    public TeacherDashboardController(ITeacherDashboardService teacherDashboardService)
    {
        _teacherDashboardService = teacherDashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var teacherId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _teacherDashboardService.GetTeacherDashboardAsync(teacherId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }
}
