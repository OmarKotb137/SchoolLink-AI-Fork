using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.Students;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[ApiController]
[Route("api/students")]
[Authorize(Roles = "Admin,Teacher,Student,Parent")]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _studentService;

    public StudentsController(IStudentService studentService)
    {
        _studentService = studentService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _studentService.GetAllStudentsAsync();
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _studentService.GetStudentByIdAsync(id);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("by-user/{userId:int}")]
    public async Task<IActionResult> GetByUserId(int userId)
    {
        var result = await _studentService.GetStudentByUserIdAsync(userId);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("search")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> Search([FromQuery] StudentSearchFilter filter)
    {
        var result = await _studentService.SearchStudentsAsync(filter);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> Create([FromBody] CreateStudentRequest request)
    {
        var result = await _studentService.CreateStudentAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    [HttpPost("bulk")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> BulkCreate([FromBody] BulkCreateStudentsRequest request)
    {
        var result = await _studentService.BulkCreateStudentsAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateStudentRequest request)
    {
        if (id != request.Id)
            return BadRequest("معرف الطالب في الرابط لا يطابق المعرف في الطلب");

        var result = await _studentService.UpdateStudentAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _studentService.DeleteStudentAsync(id);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpPost("link-user")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> LinkUser([FromBody] LinkStudentUserRequest request)
    {
        var result = await _studentService.LinkUserAccountAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }
}
