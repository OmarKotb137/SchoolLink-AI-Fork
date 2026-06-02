using Project.Domain.Enums;

namespace Project.BLL.DTOs.Users;

public class GetUsersFilter
{
    public UserRole? Role { get; set; }
    public bool? IsActive { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
