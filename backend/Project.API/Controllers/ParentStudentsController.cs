using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.ParentStudents;
using Project.Domain.Enums;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[ApiController]
[Route("api/parent-students")]
public class ParentStudentsController : ControllerBase
{
    private readonly IParentStudentService _parentStudentService;

    public ParentStudentsController(IParentStudentService parentStudentService)
    {
        _parentStudentService = parentStudentService;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Link([FromBody] LinkParentStudentRequest request)
    {
        var result = await _parentStudentService.LinkParentToStudentAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Unlink(int id)
    {
        var result = await _parentStudentService.UnlinkParentFromStudentAsync(id);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("by-parent/{parentId:int}")]
    [Authorize(Roles = "Admin,Parent")]
    public async Task<IActionResult> GetStudentsByParent(int parentId)
    {
        var result = await _parentStudentService.GetStudentsByParentAsync(parentId);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("by-student/{studentId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetParentsByStudent(int studentId)
    {
        var result = await _parentStudentService.GetParentsByStudentAsync(studentId);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpPut("{id:int}/relationship")]
    public async Task<IActionResult> UpdateRelationship(int id, [FromBody] RelationshipType relationship)
    {
        var result = await _parentStudentService.UpdateRelationshipAsync(id, relationship);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("check")]
    public async Task<IActionResult> CheckRelationship([FromQuery] int parentId, [FromQuery] int studentId)
    {
        var result = await _parentStudentService.CheckRelationshipAsync(parentId, studentId);
        return Ok(result);
    }
}
