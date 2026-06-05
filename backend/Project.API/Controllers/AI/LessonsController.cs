using Microsoft.AspNetCore.Mvc;
using Project.BLL.AI.ExamAgent.Interfaces;

namespace Project.API.Controllers.AI;

[ApiController]
[Route("api/ai/lessons")]
public class LessonsController : ControllerBase
{
    private readonly ILessonRepository _repo;

    public LessonsController(ILessonRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? subject)
        => Ok(await _repo.SearchAsync(subject));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var lesson = await _repo.GetByIdAsync(id);
        return lesson is null ? NotFound() : Ok(lesson);
    }
}
