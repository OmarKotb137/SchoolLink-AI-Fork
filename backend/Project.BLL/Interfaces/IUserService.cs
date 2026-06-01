using Common.Results;
using Project.BLL.DTOs.Common;
using Project.BLL.DTOs.Users;

namespace Project.BLL.Interfaces;

public interface IUserService
{
    Task<OperationResult<UserDto>> CreateUserAsync(CreateUserRequest request);
    Task<OperationResult<UserDto>> UpdateUserAsync(UpdateUserRequest request);
    Task<OperationResult<UserDto>> GetUserByIdAsync(int id);
    Task<OperationResult<PagedResult<UserDto>>> GetAllUsersAsync(GetUsersFilter filter);
    Task<OperationResult> SetUserActiveStatusAsync(int userId, bool isActive);
    Task<OperationResult> DeleteUserAsync(int id);
}
