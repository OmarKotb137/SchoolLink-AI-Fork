using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.SchoolProfiles;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SchoolProfileController : ControllerBase
{
    private readonly ISchoolProfileService _schoolProfileService;

    public SchoolProfileController(ISchoolProfileService schoolProfileService)
    {
        _schoolProfileService = schoolProfileService;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetActive()
    {
        var result = await _schoolProfileService.GetActiveProfileAsync();
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateSchoolProfileRequest request)
    {
        var adminUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _schoolProfileService.UpdateProfileAsync(request, adminUserId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }
}
