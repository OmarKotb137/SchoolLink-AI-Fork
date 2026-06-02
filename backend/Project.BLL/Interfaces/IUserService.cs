using Common.Results;
using Project.BLL.DTOs.Common;
using Project.BLL.DTOs.Users;
using Project.Domain.Enums;

namespace Project.BLL.Interfaces;

public interface IUserService
{
    Task<OperationResult<UserDto>> CreateUserAsync(CreateUserRequest request);
    Task<OperationResult<UserDto>> UpdateUserAsync(UpdateUserRequest request);
    Task<OperationResult<UserDto>> GetUserByIdAsync(int id);
    Task<OperationResult<PagedResult<UserDto>>> GetAllUsersAsync(GetUsersFilter filter);
    Task<OperationResult<PagedResult<UserDto>>> GetUsersByRoleAsync(UserRole role, PaginationFilter filter);
    Task<OperationResult<PagedResult<UserDto>>> SearchUsersAsync(string searchTerm, PaginationFilter filter);
    Task<OperationResult> SetUserActiveStatusAsync(int userId, bool isActive);
    Task<OperationResult> DeleteUserAsync(int id);
    Task<OperationResult> UpdateProfilePhotoAsync(int userId, string photoUrl);
}
