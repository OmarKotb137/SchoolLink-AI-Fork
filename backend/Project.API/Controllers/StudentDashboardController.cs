using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[ApiController]
[Route("api/student-dashboard")]
[Authorize(Roles = "Student")]
public class StudentDashboardController : ControllerBase
{
    private readonly IParentDashboardService _dashboardService;
    private readonly IStudentService _studentService;

    public StudentDashboardController(
        IParentDashboardService dashboardService,
        IStudentService studentService)
    {
        _dashboardService = dashboardService;
        _studentService = studentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard([FromQuery] int? term = null)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // Resolve the student record for this user
        var studentResult = await _studentService.GetStudentByUserIdAsync(userId);
        if (!studentResult.IsSuccess || studentResult.Data == null)
            return NotFound(studentResult);

        var result = await _dashboardService.GetStudentDashboardAsync(studentResult.Data.Id, term);
        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }
}
