using AutoMapper;
using Common.Results;
using Project.BLL.DTOs.Common;
using Project.BLL.DTOs.Users;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UserService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<OperationResult<UserDto>> CreateUserAsync(CreateUserRequest request)
    {
        var user = _mapper.Map<User>(request);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();
        var userDto = _mapper.Map<UserDto>(user);
        return OperationResult<UserDto>.Success(userDto, "User created successfully");
    }

    public async Task<OperationResult<UserDto>> UpdateUserAsync(UpdateUserRequest request)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);
        if (user == null)
            return OperationResult<UserDto>.Failure($"User with id {request.UserId} not found");

        user.FullName = request.FullName;
        user.Phone = request.Phone ?? user.Phone;
        user.ProfilePictureUrl = request.ProfilePictureUrl ?? user.ProfilePictureUrl;
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
        var userDto = _mapper.Map<UserDto>(user);
        return OperationResult<UserDto>.Success(userDto, "User updated successfully");
    }

    public async Task<OperationResult<UserDto>> GetUserByIdAsync(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
            return OperationResult<UserDto>.Failure($"User with id {id} not found");

        var userDto = _mapper.Map<UserDto>(user);
        return OperationResult<UserDto>.Success(userDto, "User retrieved successfully");
    }

    public async Task<OperationResult<PagedResult<UserDto>>> GetAllUsersAsync(GetUsersFilter filter)
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        var userDtos = _mapper.Map<IEnumerable<UserDto>>(users);
        var paged = new PagedResult<UserDto>
        {
            Items = userDtos,
            TotalCount = userDtos.Count(),
            Page = filter.Page,
            PageSize = filter.PageSize
        };
        return OperationResult<PagedResult<UserDto>>.Success(paged, "Users retrieved successfully");
    }

    public async Task<OperationResult<PagedResult<UserDto>>> GetUsersByRoleAsync(UserRole role, PaginationFilter filter)
    {
        var users = await _unitOfWork.Users.GetByRoleAsync(role);
        var userDtos = _mapper.Map<IEnumerable<UserDto>>(users);
        var paged = new PagedResult<UserDto>
        {
            Items = userDtos,
            TotalCount = userDtos.Count(),
            Page = filter.Page,
            PageSize = filter.PageSize
        };
        return OperationResult<PagedResult<UserDto>>.Success(paged, $"Users with role {role} retrieved successfully");
    }

    public async Task<OperationResult<PagedResult<UserDto>>> SearchUsersAsync(string searchTerm, PaginationFilter filter)
    {
        var users = await _unitOfWork.Users.SearchByNameAsync(searchTerm);
        var userDtos = _mapper.Map<IEnumerable<UserDto>>(users);
        var paged = new PagedResult<UserDto>
        {
            Items = userDtos,
            TotalCount = userDtos.Count(),
            Page = filter.Page,
            PageSize = filter.PageSize
        };
        return OperationResult<PagedResult<UserDto>>.Success(paged, "Search completed successfully");
    }

    public async Task<OperationResult> SetUserActiveStatusAsync(int userId, bool isActive)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return OperationResult.Failure($"User with id {userId} not found");

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
        return OperationResult.Success(isActive ? "User activated successfully" : "User deactivated successfully");
    }

    public async Task<OperationResult> UpdateProfilePhotoAsync(int userId, string photoUrl)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
            return OperationResult.Failure($"User with id {userId} not found");

        user.ProfilePictureUrl = photoUrl;
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
        return OperationResult.Success("Profile photo updated successfully");
    }

    public async Task<OperationResult<UserStatsDto>> GetUserStatsAsync()
    {
        var total = await _unitOfWork.Users.CountAsync(u => !u.IsDeleted);
        var admins = await _unitOfWork.Users.GetCountByRoleAsync(UserRole.Admin);
        var teachers = await _unitOfWork.Users.GetCountByRoleAsync(UserRole.Teacher);
        var students = await _unitOfWork.Users.GetCountByRoleAsync(UserRole.Student);
        var parents = await _unitOfWork.Users.GetCountByRoleAsync(UserRole.Parent);

        var stats = new UserStatsDto
        {
            Total = total,
            Admins = admins,
            Teachers = teachers,
            Students = students,
            Parents = parents
        };

        return OperationResult<UserStatsDto>.Success(stats);
    }

    public async Task<OperationResult> BulkDeleteUsersAsync(List<int> userIds)
    {
        foreach (var id in userIds)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null) continue;

            user.IsDeleted = true;
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Users.Update(user);
        }

        await _unitOfWork.SaveChangesAsync();
        return OperationResult.Success($"{userIds.Count} users deleted successfully");
    }

    public async Task<OperationResult<UserDto>> UpdateProfileAsync(int userId, UpdateProfileRequest request)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return OperationResult<UserDto>.Failure("User not found");

        user.FullName = request.FullName;
        user.Phone = request.Phone;
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<UserDto>(user);
        return OperationResult<UserDto>.Success(dto, "Profile updated successfully");
    }

    public async Task<OperationResult> DeleteProfilePhotoAsync(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return OperationResult.Failure("User not found");

        user.ProfilePictureUrl = null;
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("Profile photo removed successfully");
    }

    public async Task<OperationResult> DeleteUserAsync(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
            return OperationResult.Failure($"User with id {id} not found");

        user.IsDeleted = true;
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
        return OperationResult.Success("User deleted successfully");
    }
}