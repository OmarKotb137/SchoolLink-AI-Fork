using AutoMapper;
using Common.Results;
using Project.BLL.DTOs;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using SchoolLink.Domain.Entities;

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

    public async Task<OperationResult<IEnumerable<UserDto>>> GetAllAsync()
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        var userDtos = _mapper.Map<IEnumerable<UserDto>>(users);
        return OperationResult<IEnumerable<UserDto>>.Success(userDtos, "Users retrieved successfully");
    }

    public async Task<OperationResult<UserDto>> GetByIdAsync(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
            return OperationResult<UserDto>.Failure($"User with id {id} not found");

        var userDto = _mapper.Map<UserDto>(user);
        return OperationResult<UserDto>.Success(userDto, "User retrieved successfully");
    }

    public async Task<OperationResult<UserDto>> CreateAsync(CreateUserDto dto)
    {
        var user = _mapper.Map<User>(dto);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();
        var userDto = _mapper.Map<UserDto>(user);
        return OperationResult<UserDto>.Success(userDto, "User created successfully");
    }

    public async Task<OperationResult<UserDto>> UpdateAsync(int id, UpdateUserDto dto)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
            return OperationResult<UserDto>.Failure($"User with id {id} not found");

        user.FullName = $"{dto.FirstName} {dto.LastName}".Trim();
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
        var userDto = _mapper.Map<UserDto>(user);
        return OperationResult<UserDto>.Success(userDto, "User updated successfully");
    }

    public async Task<OperationResult> DeleteAsync(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
            return OperationResult.Failure($"User with id {id} not found");

        user.IsDeleted = true;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
        return OperationResult.Success("User deleted successfully");
    }
}