using Project.Domain.Enums;

namespace Project.BLL.DTOs.Users;

public class CreateUserRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; }
}
