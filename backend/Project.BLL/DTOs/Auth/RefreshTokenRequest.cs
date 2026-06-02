using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs.Auth;

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "Expired access token is required")]
    public string ExpiredAccessToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}
