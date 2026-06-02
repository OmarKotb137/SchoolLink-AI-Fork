using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    private readonly IUserService _userService;

    public ProfileController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("upload-photo/{userId}")]
    public async Task<IActionResult> UploadPhoto(int userId, IFormFile file)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var currentUserRole = User.FindFirst(ClaimTypes.Role)!.Value;

        if (currentUserId != userId && currentUserRole != "Admin")
            return Forbid();

        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file provided" });

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
            return BadRequest(new { error = "Only image files are allowed (jpg, jpeg, png, gif, webp)" });

        var fileName = $"{userId}_{Guid.NewGuid()}{ext}";
        var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
        Directory.CreateDirectory(dir);

        var filePath = Path.Combine(dir, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var photoUrl = $"/uploads/profiles/{fileName}";

        var result = await _userService.UpdateProfilePhotoAsync(userId, photoUrl);
        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(new { photoUrl });
    }
}
