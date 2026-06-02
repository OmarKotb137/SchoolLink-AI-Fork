using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs.Auth;

public class ChangePasswordRequest
{
    public int UserId { get; set; }

    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "New password must be at least 6 characters")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password confirmation is required")]
    [Compare("NewPassword", ErrorMessage = "New password and confirmation do not match")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
