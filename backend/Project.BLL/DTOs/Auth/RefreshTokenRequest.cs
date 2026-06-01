namespace Project.BLL.DTOs.Auth;

public class RefreshTokenRequest
{
    public string ExpiredAccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}
