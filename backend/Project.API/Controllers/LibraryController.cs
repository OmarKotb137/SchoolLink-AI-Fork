using Microsoft.AspNetCore.Mvc;
using Project.BLL.Interfaces;
using Project.BLL.DTOs.Library;

namespace Project.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LibraryController : ControllerBase
{
    private readonly ILibraryService _libraryService;
    private readonly IDropboxService _dropboxService;

    public LibraryController(ILibraryService libraryService, IDropboxService dropboxService)
    {
        _libraryService = libraryService;
        _dropboxService = dropboxService;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromForm] string title,
        [FromForm] int itemType,
        [FromForm] int uploadedById,
        [FromForm] int? subjectId = null,
        [FromForm] int? gradeLevelId = null,
        [FromForm] int? academicYearId = null,
        [FromForm] string? description = null)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file provided" });

        using var stream = file.OpenReadStream();
        var dropboxResult = await _dropboxService.UploadFileAsync(stream, file.FileName);

        if (!dropboxResult.IsSuccess)
            return BadRequest(dropboxResult);

        var request = new CreateLibraryItemRequest
        {
            Title = title,
            Description = description,
            ItemType = (Project.Domain.Enums.LibraryItemType)itemType,
            FileUrl = dropboxResult.Data,
            SubjectId = subjectId,
            GradeLevelId = gradeLevelId,
            AcademicYearId = academicYearId,
            UploadedById = uploadedById
        };

        var result = await _libraryService.CreateLibraryItemAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetLibraryFilter filter)
    {
        var result = await _libraryService.GetLibraryItemsAsync(filter);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int callerUserId)
    {
        var result = await _libraryService.DeleteLibraryItemAsync(id, callerUserId);
        if (!result.IsSuccess)
            return NotFound(result);
        return NoContent();
    }
}
