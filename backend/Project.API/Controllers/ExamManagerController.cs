using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.BLL.DTOs.Exam;
using Project.BLL.Interfaces;
using Project.DAL.Context;

namespace Project.API.Controllers;

[ApiController]
[Route("api/exam-manager")]
[Authorize]
public class ExamManagerController : ControllerBase
{
    private readonly IExamManagerService _service;
    private readonly AppDbContext _context;

    public ExamManagerController(IExamManagerService service, AppDbContext context)
    {
        _service = service;
        _context = context;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] int? subjectId,
        [FromQuery] string? status,
        [FromQuery] string? sortBy,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? academicYearId = null)
    {
        var userId = GetUserId();

        // نجيب CSTs + SubjectIds للمواد التي يُدرّسها المعلم (للأمتحانات CST=null)
        var cstQuery = _context.ClassSubjectTeachers
            .Where(c => c.TeacherId == userId && !c.IsDeleted);

        if (academicYearId.HasValue)
            cstQuery = cstQuery.Where(c => c.AcademicYearId == academicYearId.Value);

        var cstData = await cstQuery
            .Select(c => new { c.Id, c.SubjectId })
            .ToListAsync();

        var cstIds = cstData.Select(c => c.Id).Distinct().ToList();
        var subjectIds = cstData.Select(c => c.SubjectId).Distinct().ToList();

        var filter = new ExamManagerFilterDto
        {
            Search = search,
            SubjectId = subjectId,
            Status = status,
            SortBy = sortBy,
            Page = page,
            PageSize = pageSize,
            CstIds = cstIds.Count > 0 ? cstIds : null,
            SubjectIds = subjectIds.Count > 0 ? subjectIds : null
        };

        var result = await _service.GetAllAsync(filter);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> Create([FromBody] CreateExamManagerDto dto)
    {
        var teacherId = GetUserId();
        var result = await _service.CreateAsync(dto, teacherId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateExamManagerDto dto)
    {
        var result = await _service.UpdateAsync(id, dto, GetUserId());
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(id);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("{id:int}/publish")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> Publish(int id)
    {
        var result = await _service.PublishAsync(id, GetUserId());
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("{id:int}/publish-results")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> PublishResults(int id)
    {
        var result = await _service.ToggleResultPublishStatusAsync(id, true, GetUserId());
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("{id:int}/unpublish-results")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> UnpublishResults(int id)
    {
        var result = await _service.ToggleResultPublishStatusAsync(id, false, GetUserId());
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats([FromQuery] int? academicYearId)
    {
        var userId = GetUserId();

        var cstQuery = _context.ClassSubjectTeachers
            .Where(c => c.TeacherId == userId && !c.IsDeleted);

        if (academicYearId.HasValue)
            cstQuery = cstQuery.Where(c => c.AcademicYearId == academicYearId.Value);

        var cstData = await cstQuery
            .Select(c => new { c.Id, c.SubjectId })
            .ToListAsync();

        var cstIds = cstData.Select(c => c.Id).Distinct().ToList();
        var subjectIds = cstData.Select(c => c.SubjectId).Distinct().ToList();

        var result = await _service.GetStatsAsync(
            cstIds.Count > 0 ? cstIds : null,
            subjectIds.Count > 0 ? subjectIds : null);
        return Ok(result);
    }

    [HttpGet("subjects")]
    public async Task<IActionResult> GetSubjects()
    {
        var userId = GetUserId();
        var role   = User.FindFirstValue(ClaimTypes.Role);

        if (role == "Teacher")
        {
            var subjectIds = await _context.ClassSubjectTeachers
                .Where(c => c.TeacherId == userId && !c.IsDeleted)
                .Select(c => c.SubjectId)
                .Distinct()
                .ToListAsync();

            var subjects = await _context.Subjects
                .Where(s => subjectIds.Contains(s.Id) && !s.IsDeleted)
                .Select(s => new { s.Id, s.Name })
                .ToListAsync();

            return Ok(subjects);
        }

        var allSubjects = await _context.Subjects
            .Where(s => !s.IsDeleted)
            .Select(s => new { s.Id, s.Name })
            .ToListAsync();
        return Ok(allSubjects);
    }

    [HttpGet("classes")]
    public async Task<IActionResult> GetClasses([FromQuery] int? subjectId = null)
    {
        var userId = GetUserId();
        var role   = User.FindFirstValue(ClaimTypes.Role);

        if (role == "Teacher")
        {
            var cstQuery = _context.ClassSubjectTeachers
                .Where(c => c.TeacherId == userId && !c.IsDeleted);

            if (subjectId.HasValue)
                cstQuery = cstQuery.Where(c => c.SubjectId == subjectId.Value);

            var classIds = await cstQuery
                .Select(c => c.ClassId)
                .Distinct()
                .ToListAsync();

            var classes = await _context.Classes
                .Include(c => c.GradeLevel)
                .Where(c => classIds.Contains(c.Id) && !c.IsDeleted)
                .Select(c => new { c.Id, Name = c.GradeLevel.Name + " - " + c.Name, c.GradeLevelId })
                .ToListAsync();

            return Ok(classes);
        }

        var allClassesQuery = _context.Classes
            .Include(c => c.GradeLevel)
            .Where(c => !c.IsDeleted);

        if (subjectId.HasValue)
        {
            var classIdsForSubject = await _context.ClassSubjectTeachers
                .Where(c => c.SubjectId == subjectId.Value && !c.IsDeleted)
                .Select(c => c.ClassId)
                .Distinct()
                .ToListAsync();
            allClassesQuery = allClassesQuery.Where(c => classIdsForSubject.Contains(c.Id));
        }

        var allClasses = await allClassesQuery
            .Select(c => new { c.Id, Name = c.GradeLevel.Name + " - " + c.Name, c.GradeLevelId })
            .ToListAsync();
        return Ok(allClasses);
    }
}
