using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs.Auth;

public class LogoutRequest
{
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}
