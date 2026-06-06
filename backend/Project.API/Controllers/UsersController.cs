using System.Security.Claims;
using Common.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.Common;
using Project.BLL.DTOs.Users;
using Project.BLL.Interfaces;
using Project.Domain.Enums;

namespace Project.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _userService.GetUserByIdAsync(userId);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetUsersFilter filter)
    {
        var result = await _userService.GetAllUsersAsync(filter);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _userService.GetUserByIdAsync(id);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("role/{role}")]
    public async Task<IActionResult> GetByRole(Project.Domain.Enums.UserRole role, [FromQuery] PaginationFilter filter)
    {
        var result = await _userService.GetUsersByRoleAsync(role, filter);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var result = await _userService.GetUserStatsAsync();
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string term, [FromQuery] PaginationFilter filter)
    {
        var result = await _userService.SearchUsersAsync(term, filter);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var result = await _userService.CreateUserAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
    {
        request.UserId = id;
        var result = await _userService.UpdateUserAsync(request);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id}/active-status")]
    public async Task<IActionResult> SetActiveStatus(int id, [FromBody] bool isActive)
    {
        var result = await _userService.SetUserActiveStatusAsync(id, isActive);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("bulk-delete")]
    public async Task<IActionResult> BulkDelete([FromBody] List<int> userIds)
    {
        var result = await _userService.BulkDeleteUsersAsync(userIds);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _userService.DeleteUserAsync(id);
        if (!result.IsSuccess)
            return NotFound(result);
        return NoContent();
    }
}
