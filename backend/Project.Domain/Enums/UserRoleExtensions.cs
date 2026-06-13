namespace Project.Domain.Enums;

public static class UserRoleExtensions
{
    public static bool IsAdminLike(this UserRole role)
    => role is UserRole.Admin;
}
