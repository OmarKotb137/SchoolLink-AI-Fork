using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    public async Task<IActionResult> GetAll([FromQuery] int? academicYearId)
    {
        int? cstId = null;
        var userId = GetUserId();
        if (academicYearId.HasValue)
        {
            var csts = await _context.ClassSubjectTeachers
                .Where(c => c.TeacherId == userId && c.AcademicYearId == academicYearId.Value && !c.IsDeleted)
                .Select(c => c.Id)
                .ToListAsync();
            if (csts.Count == 1)
                cstId = csts[0];
        }

        var result = await _service.GetAllAsync(cstId);
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
        var result = await _service.UpdateAsync(id, dto);
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
        int? cstId = null;
        var userId = GetUserId();
        if (academicYearId.HasValue)
        {
            var csts = await _context.ClassSubjectTeachers
                .Where(c => c.TeacherId == userId && c.AcademicYearId == academicYearId.Value && !c.IsDeleted)
                .Select(c => c.Id)
                .ToListAsync();
            if (csts.Count == 1)
                cstId = csts[0];
        }

        var result = await _service.GetStatsAsync(cstId);
        return Ok(result);
    }

    [HttpGet("subjects")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSubjects()
    {
        var subjects = await _context.Subjects
            .Where(s => !s.IsDeleted)
            .Select(s => new { s.Id, s.Name })
            .ToListAsync();
        return Ok(subjects);
    }

    [HttpGet("classes")]
    [AllowAnonymous]
    public async Task<IActionResult> GetClasses()
    {
        var classes = await _context.Classes
            .Include(c => c.GradeLevel)
            .Where(c => !c.IsDeleted)
            .Select(c => new { c.Id, Name = c.GradeLevel.Name + " - " + c.Name })
            .ToListAsync();
        return Ok(classes);
    }
}
