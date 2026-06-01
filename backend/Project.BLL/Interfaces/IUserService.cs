using Common.Results;
using Project.BLL.DTOs;

namespace Project.BLL.Interfaces;

public interface IUserService
{
    Task<OperationResult<IEnumerable<UserDto>>> GetAllAsync();
    Task<OperationResult<UserDto>> GetByIdAsync(int id);
    Task<OperationResult<UserDto>> CreateAsync(CreateUserDto dto);
    Task<OperationResult<UserDto>> UpdateAsync(int id, UpdateUserDto dto);
    Task<OperationResult> DeleteAsync(int id);
}
