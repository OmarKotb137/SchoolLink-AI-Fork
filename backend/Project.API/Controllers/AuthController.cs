using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.Auth;
using Project.BLL.Interfaces;
using Project.Domain.Enums;

namespace Project.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login/admin")]
    public async Task<IActionResult> LoginAsAdmin([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginByRoleAsync(request, UserRole.Admin);
        if (!result.IsSuccess)
            return Unauthorized(result);
        return Ok(result);
    }

    [HttpPost("login/teacher")]
    public async Task<IActionResult> LoginAsTeacher([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginByRoleAsync(request, UserRole.Teacher);
        if (!result.IsSuccess)
            return Unauthorized(result);
        return Ok(result);
    }

    [HttpPost("login/parent")]
    public async Task<IActionResult> LoginAsParent([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginByRoleAsync(request, UserRole.Parent);
        if (!result.IsSuccess)
            return Unauthorized(result);
        return Ok(result);
    }

    [HttpPost("login/student")]
    public async Task<IActionResult> LoginAsStudent([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginByRoleAsync(request, UserRole.Student);
        if (!result.IsSuccess)
            return Unauthorized(result);
        return Ok(result);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request);
        if (!result.IsSuccess)
            return Unauthorized(result);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _authService.LogoutAsync(request, userId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        request.UserId = userId;
        var result = await _authService.ChangePasswordAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }
}
