using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[ApiController]
[Route("api/subjects")]
public class SubjectController : ControllerBase
{
    private readonly ISubjectService _subjectService;
    private readonly IAcademicYearService _academicYearService;

    public SubjectController(ISubjectService subjectService, IAcademicYearService academicYearService)
    {
        _subjectService = subjectService;
        _academicYearService = academicYearService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _subjectService.GetAllSubjectsAsync();
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _subjectService.GetSubjectByIdAsync(id);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string term)
    {
        var result = await _subjectService.SearchSubjectsAsync(term);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,Teacher")]
    [HttpGet("by-grade-level/{gradeLevelId:int}")]
    public async Task<IActionResult> GetByGradeLevel(int gradeLevelId)
    {
        var result = await _subjectService.GetSubjectsByGradeLevelAsync(gradeLevelId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("by-teacher/{teacherId:int}")]
    public async Task<IActionResult> GetByTeacher(int teacherId, [FromQuery] int academicYearId)
    {
        if (!User.IsInRole("Admin"))
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            if (currentUserId != teacherId)
                return Forbid();
        }

        var result = await _subjectService.GetSubjectsByTeacherAsync(teacherId, academicYearId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Roles = "Teacher")]
    [HttpGet("my-subjects")]
    public async Task<IActionResult> GetMySubjects([FromQuery] int academicYearId)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _subjectService.GetSubjectsByTeacherAsync(currentUserId, academicYearId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Roles = "Teacher")]
    [HttpGet("my-subjects/current-year")]
    public async Task<IActionResult> GetMySubjectsCurrentYear()
    {
        var currentYearResult = await _academicYearService.GetCurrentAcademicYearAsync();
        if (!currentYearResult.IsSuccess || currentYearResult.Data is null)
            return NotFound(currentYearResult);

        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _subjectService.GetSubjectsByTeacherAsync(currentUserId, currentYearResult.Data.Id);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSubjectRequest request)
    {
        var result = await _subjectService.CreateSubjectAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSubjectRequest request)
    {
        if (id != request.Id)
            return BadRequest("معرّف الرابط لا يطابق معرّف الطلب.");

        var result = await _subjectService.UpdateSubjectAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _subjectService.DeleteSubjectAsync(id);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }
}
