using Microsoft.AspNetCore.Mvc;
using Project.DAL.Interfaces;

namespace Project.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentsController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public StudentsController(IUnitOfWork uow) { _uow = uow; }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var students = await _uow.Students.GetActiveStudentsAsync();
        var result = students.Select(s => new { id = s.Id, name = s.FullName }).OrderBy(s => s.name).ToList();
        return Ok(new { isSuccess = true, data = result });
    }

    [HttpGet("available-for-class/{classId:int}")]
    public async Task<IActionResult> GetAvailableForClass(int classId)
    {
        var cls = await _uow.Classes.GetByIdAsync(classId);
        if (cls is null || cls.IsDeleted)
            return NotFound(new { isSuccess = false, message = "الفصل غير موجود" });

        var enrolledIds = (await _uow.StudentEnrollments.FindAsync(
            e => e.ClassId == classId && e.AcademicYearId == cls.AcademicYearId))
            .Select(e => e.StudentId)
            .ToHashSet();

        var all = await _uow.Students.GetActiveStudentsAsync();
        var result = all
            .Where(s => !enrolledIds.Contains(s.Id))
            .Select(s => new { id = s.Id, name = s.FullName })
            .OrderBy(s => s.name)
            .ToList();

        return Ok(new { isSuccess = true, data = result });
    }
}
