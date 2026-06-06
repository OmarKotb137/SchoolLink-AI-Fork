using AutoMapper;
using Common.Results;
using Microsoft.EntityFrameworkCore;
using Project.BLL.DTOs.Common;
using Project.BLL.DTOs.Users;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Enums;
using Project.Domain.Entities;

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
        if (request.Role == UserRole.Teacher)
            return OperationResult<UserDto>.Failure("استخدم API المعلمين لإنشاء حسابات المعلمين وتخصصاتهم");

        var existing = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        if (existing != null && !existing.IsDeleted)
            return OperationResult<UserDto>.Failure("يوجد مستخدم مسجل بهذا البريد الإلكتروني بالفعل");

        if (existing != null && existing.IsDeleted)
            return OperationResult<UserDto>.Failure("هذا البريد الإلكتروني مرتبط بحساب محذوف، يرجى استخدام بريد إلكتروني آخر");

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var user = _mapper.Map<User>(request);
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            await _unitOfWork.CommitTransactionAsync();

            var userDto = _mapper.Map<UserDto>(user);
            return OperationResult<UserDto>.Success(userDto, "تم إنشاء المستخدم بنجاح");
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Users_Email") == true)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return OperationResult<UserDto>.Failure("يوجد مستخدم مسجل بهذا البريد الإلكتروني بالفعل");
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<OperationResult<UserDto>> UpdateUserAsync(UpdateUserRequest request)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);
        if (user == null || user.IsDeleted)
            return OperationResult<UserDto>.Failure($"لم يتم العثور على مستخدم بالمعرف {request.UserId}");

        if (user.Role == UserRole.Teacher)
            return OperationResult<UserDto>.Failure("استخدم API المعلمين لتعديل بيانات المعلمين وتخصصاتهم");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            user.FullName = request.FullName;
            user.Phone = request.Phone ?? user.Phone;
            user.ProfilePictureUrl = request.ProfilePictureUrl ?? user.ProfilePictureUrl;
            user.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            await _unitOfWork.CommitTransactionAsync();
            var userDto = _mapper.Map<UserDto>(user);
            return OperationResult<UserDto>.Success(userDto, "تم تحديث المستخدم بنجاح");
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<OperationResult<UserDto>> GetUserByIdAsync(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null || user.IsDeleted)
            return OperationResult<UserDto>.Failure($"لم يتم العثور على مستخدم بالمعرف {id}");

        var userDto = _mapper.Map<UserDto>(user);
        return OperationResult<UserDto>.Success(userDto, "تم استرجاع المستخدم بنجاح");
    }

    public async Task<OperationResult<PagedResult<UserDto>>> GetAllUsersAsync(GetUsersFilter filter)
    {
        var users = await _unitOfWork.Users.FindAsync(u => !u.IsDeleted);
        var userDtos = _mapper.Map<IEnumerable<UserDto>>(users);
        var paged = new PagedResult<UserDto>
        {
            Items = userDtos,
            TotalCount = userDtos.Count(),
            Page = filter.Page,
            PageSize = filter.PageSize
        };
        return OperationResult<PagedResult<UserDto>>.Success(paged, "تم استرجاع المستخدمين بنجاح");
    }

    public async Task<OperationResult<PagedResult<UserDto>>> GetUsersByRoleAsync(UserRole role, PaginationFilter filter)
    {
        var users = await _unitOfWork.Users.FindAsync(u => u.Role == role && !u.IsDeleted);
        var userDtos = _mapper.Map<IEnumerable<UserDto>>(users);
        var paged = new PagedResult<UserDto>
        {
            Items = userDtos,
            TotalCount = userDtos.Count(),
            Page = filter.Page,
            PageSize = filter.PageSize
        };
        return OperationResult<PagedResult<UserDto>>.Success(paged, $"تم استرجاع المستخدمين ذوي الدور {role} بنجاح");
    }

    public async Task<OperationResult<PagedResult<UserDto>>> SearchUsersAsync(string searchTerm, PaginationFilter filter)
    {
        var users = await _unitOfWork.Users.FindAsync(u => u.FullName.Contains(searchTerm) && !u.IsDeleted);
        var userDtos = _mapper.Map<IEnumerable<UserDto>>(users);
        var paged = new PagedResult<UserDto>
        {
            Items = userDtos,
            TotalCount = userDtos.Count(),
            Page = filter.Page,
            PageSize = filter.PageSize
        };
        return OperationResult<PagedResult<UserDto>>.Success(paged, "تم إكمال البحث بنجاح");
    }

    public async Task<OperationResult> SetUserActiveStatusAsync(int userId, bool isActive)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
            return OperationResult.Failure($"لم يتم العثور على مستخدم بالمعرف {userId}");

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
        return OperationResult.Success(isActive ? "تم تنشيط المستخدم بنجاح" : "تم إلغاء تنشيط المستخدم بنجاح");
    }

    public async Task<OperationResult> UpdateProfilePhotoAsync(int userId, string photoUrl)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
            return OperationResult.Failure($"لم يتم العثور على مستخدم بالمعرف {userId}");

        user.ProfilePictureUrl = photoUrl;
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
        return OperationResult.Success("تم تحديث الصورة الشخصية بنجاح");
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

            if (user.Role == UserRole.Teacher)
                return OperationResult.Failure("استخدم API المعلمين لإدارة حذف أو تعطيل المعلمين");

            user.IsDeleted = true;
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Users.Update(user);
        }

        await _unitOfWork.SaveChangesAsync();
        return OperationResult.Success($"تم حذف {userIds.Count} مستخدمين بنجاح");
    }

    public async Task<OperationResult<UserDto>> UpdateProfileAsync(int userId, UpdateProfileRequest request)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
            return OperationResult<UserDto>.Failure("المستخدم غير موجود");

        user.FullName = request.FullName;
        user.Phone = request.Phone;
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<UserDto>(user);
        return OperationResult<UserDto>.Success(dto, "تم تحديث الملف الشخصي بنجاح");
    }

    public async Task<OperationResult> DeleteProfilePhotoAsync(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
            return OperationResult.Failure("المستخدم غير موجود");

        user.ProfilePictureUrl = null;
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("تم إزالة الصورة الشخصية بنجاح");
    }

    public async Task<OperationResult> DeleteUserAsync(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null || user.IsDeleted)
            return OperationResult.Failure($"لم يتم العثور على مستخدم بالمعرف {id}");

        if (user.Role == UserRole.Teacher)
            return OperationResult.Failure("استخدم API المعلمين لإدارة حذف أو تعطيل المعلمين");

        user.IsDeleted = true;
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);

        await _unitOfWork.SaveChangesAsync();
        return OperationResult.Success("تم حذف المستخدم بنجاح");
    }

    public async Task<OperationResult<UserDto>> GetUserByEmailAsync(string email)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(email);
        if (user == null || user.IsDeleted)
            return OperationResult<UserDto>.Failure($"لم يتم العثور على مستخدم بالبريد الإلكتروني {email}");

        var dto = _mapper.Map<UserDto>(user);
        return OperationResult<UserDto>.Success(dto, "تم استرجاع المستخدم بنجاح");
    }

    public async Task<OperationResult<IEnumerable<UserDto>>> GetStudentsByParentAsync(int parentId)
    {
        var parent = await _unitOfWork.Users.GetByIdAsync(parentId);
        if (parent == null || parent.IsDeleted)
            return OperationResult<IEnumerable<UserDto>>.Failure("لم يتم العثور على ولي الأمر");

        var links = await _unitOfWork.ParentStudents.GetWithStudentDetailsByParentAsync(parentId);
        var students = links.Where(l => !l.IsDeleted && l.Student != null && !l.Student.IsDeleted)
                            .Select(l => l.Student!);

        var dtos = _mapper.Map<IEnumerable<UserDto>>(students);
        return OperationResult<IEnumerable<UserDto>>.Success(dtos, "تم استرجاع الطلاب بنجاح");
    }

    public async Task<OperationResult<IEnumerable<UserDto>>> ExportUsersAsync(UserRole? role = null)
    {
        IReadOnlyList<User> users;
        if (role.HasValue)
            users = await _unitOfWork.Users.GetByRoleAsync(role.Value);
        else
            users = await _unitOfWork.Users.GetAllAsync();

        var filtered = users.Where(u => !u.IsDeleted).OrderBy(u => u.FullName);
        var dtos = _mapper.Map<IEnumerable<UserDto>>(filtered);
        return OperationResult<IEnumerable<UserDto>>.Success(dtos, "تم تصدير المستخدمين بنجاح");
    }
}
