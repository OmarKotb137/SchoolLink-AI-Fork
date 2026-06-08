namespace Project.BLL.DTOs.Users;

public class ResetPasswordResult
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
