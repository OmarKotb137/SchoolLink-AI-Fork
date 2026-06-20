using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.Interfaces;
using Project.Domain.Enums;

namespace Project.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin,Teacher,Student,Parent")]
public class AcademicReportController : ControllerBase
{
    private readonly IAcademicReportService _reportService;

    public AcademicReportController(IAcademicReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// Consolidated academic report – returns weekly scores, final grades,
    /// monthly exams, and summary stats for a class/term/subject.
    /// Optionally pass gradeLevelId to aggregate across all classes of a grade level.
    /// </summary>
    [HttpGet("academic")]
    public async Task<IActionResult> GetAcademicReport(
        [FromQuery] int classId,
        [FromQuery] AcademicTerm term = AcademicTerm.FirstSemester,
        [FromQuery] int subjectId = 0,
        [FromQuery] int? gradeLevelId = null)
    {
        if (subjectId == 0)
            return BadRequest(new { message = "subjectId is required" });

        var result = await _reportService.GetAcademicReportAsync(classId, term, subjectId, gradeLevelId);
        if (!result.IsSuccess)
            return NotFound(result);

        return Ok(result);
    }
}
