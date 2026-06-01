namespace Project.BLL.DTOs.Users;

public class UpdateUserRequest
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? ProfilePictureUrl { get; set; }
}
