using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.EmailVerification;
using Project.BLL.DTOs.Users;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    private readonly IEmailOtpService _emailOtpService;
    private readonly IUserService _userService;

    public ProfileController(IUserService userService, IEmailOtpService emailOtpService)
    {
        _userService = userService;
        _emailOtpService = emailOtpService;
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

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _userService.UpdateProfileAsync(userId, request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("photo")]
    public async Task<IActionResult> DeletePhoto()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _userService.DeleteProfilePhotoAsync(userId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    private static readonly string[] AllowedPhotoExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private static readonly string[] AllowedPhotoMimeTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };

    [HttpPost("upload-photo/{userId}")]
    public async Task<IActionResult> UploadPhoto(int userId, IFormFile file)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var currentUserRole = User.FindFirst(ClaimTypes.Role)!.Value;

        if (currentUserId != userId && currentUserRole != "Admin")
            return Forbid();

        if (file == null || file.Length == 0)
            return BadRequest(new { error = "لم يتم إرسال صورة" });

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { error = "حجم الصورة يجب ألا يتجاوز 5 ميجابايت" });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedPhotoExtensions.Contains(ext))
            return BadRequest(new { error = "يسمح فقط بملفات الصور (jpg, jpeg, png, gif, webp)" });

        if (!AllowedPhotoMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
            return BadRequest(new { error = "نوع ملف الصورة غير صالح" });

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

    [HttpPost("email/send-otp")]
    public async Task<IActionResult> SendEmailOtp([FromBody] SendEmailOtpRequest request, CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _emailOtpService.SendVerificationOtpAsync(userId, request.Email, ct);
        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("email/verify-otp")]
    public async Task<IActionResult> VerifyEmailOtp([FromBody] VerifyEmailOtpRequest request, CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _emailOtpService.VerifyEmailOtpAsync(userId, request.Email, request.Code, ct);
        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }
}
