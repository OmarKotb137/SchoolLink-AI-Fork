using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[Route("api/child-progress")]
[ApiController]
[Authorize(Roles = "Parent")]
public class ChildProgressController : ControllerBase
{
    private readonly IChildProgressService _service;

    public ChildProgressController(IChildProgressService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var parentId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.GetChildProgressAsync(parentId);
        return Ok(result);
    }
}
