using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.ClassStudentsBrowser;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[ApiController]
[Route("api/class-students-browser")]
[Authorize(Roles = "Admin,Teacher")]
public class ClassStudentsBrowserController : ControllerBase
{
    private readonly IClassStudentsBrowserService _classStudentsBrowserService;
    private readonly IClassService _classService;
    private readonly IAcademicYearService _academicYearService;

    public ClassStudentsBrowserController(
        IClassStudentsBrowserService classStudentsBrowserService,
        IClassService classService,
        IAcademicYearService academicYearService)
    {
        _classStudentsBrowserService = classStudentsBrowserService;
        _classService = classService;
        _academicYearService = academicYearService;
    }

    [HttpGet("{classId:int}/students")]
    public async Task<IActionResult> GetClassStudents(
        int classId,
        [FromQuery] GetClassStudentsBrowserFilter filter)
    {
        var academicYearId = filter.AcademicYearId;
        if (academicYearId <= 0)
        {
            var currentYearResult = await _academicYearService.GetCurrentAcademicYearAsync();
            if (!currentYearResult.IsSuccess || currentYearResult.Data is null)
                return NotFound(currentYearResult);

            academicYearId = currentYearResult.Data.Id;
            filter.AcademicYearId = academicYearId;
        }

        if (!User.IsInRole("Admin"))
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim is null)
                return Forbid();

            var teacherClassesResult = await _classService.GetClassesByTeacherAsync(
                int.Parse(userIdClaim.Value),
                academicYearId);

            if (!teacherClassesResult.IsSuccess)
                return BadRequest(teacherClassesResult);

            if (!(teacherClassesResult.Data?.Any(c => c.Id == classId) ?? false))
                return Forbid();
        }

        var result = await _classStudentsBrowserService.GetClassStudentsAsync(classId, filter);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }
}
