using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.Interfaces;
using Project.BLL.DTOs.Library;

namespace Project.API.Controllers;

[Authorize]
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

    private static readonly string[] AllowedUploadMimeTypes = new[]
    {
        "application/pdf", "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "text/plain", "video/mp4", "video/webm"
    };

    private static readonly string[] AllowedUploadExtensions = new[]
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".jpg", ".jpeg", ".png", ".gif", ".webp",
        ".txt", ".mp4", ".webm"
    };

    [Authorize(Roles = "Admin,Teacher")]
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromForm] string title,
        [FromForm] int itemType,
        [FromForm] int? subjectId = null,
        [FromForm] int? gradeLevelId = null,
        [FromForm] int? academicYearId = null,
        [FromForm] string? description = null)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file provided" });

        if (file.Length > 500 * 1024 * 1024)
            return BadRequest(new { error = "File size must not exceed 500 MB" });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedUploadExtensions.Contains(ext))
            return BadRequest(new { error = $"File extension '{ext}' is not allowed" });

        if (!AllowedUploadMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
            return BadRequest(new { error = $"File type '{file.ContentType}' is not allowed" });

        var uploadedById = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        using var stream = file.OpenReadStream();
        var dropboxResult = await _dropboxService.UploadFileAsync(stream, file.FileName);

        if (!dropboxResult.IsSuccess)
            return BadRequest(dropboxResult);

        var request = new CreateLibraryItemRequest
        {
            Title = title,
            Description = description,
            ItemType = (SchoolLink.Domain.Enums.LibraryItemType)itemType,
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

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _libraryService.GetLibraryItemByIdAsync(id);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var result = await _libraryService.GetLibraryStatsAsync();
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatest([FromQuery] int count = 5)
    {
        var result = await _libraryService.GetLatestLibraryItemsAsync(count);
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

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateLibraryItemRequest request)
    {
        request.Id = id;
        request.CallerUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _libraryService.UpdateLibraryItemAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string term, [FromQuery] int gradeLevelId)
    {
        var result = await _libraryService.SearchLibraryAsync(term, gradeLevelId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var callerUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _libraryService.DeleteLibraryItemAsync(id, callerUserId);
        if (!result.IsSuccess)
            return NotFound(result);
        return NoContent();
    }
}
