using AutoMapper;
using Common.Results;
using Microsoft.EntityFrameworkCore;
using Project.BLL.DTOs.Common;
using Project.BLL.DTOs.Teachers;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.Services;

public class TeacherService : ITeacherService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public TeacherService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<OperationResult<TeacherDto>> CreateTeacherAsync(CreateTeacherRequest request)
    {
        var existing = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        if (existing != null && !existing.IsDeleted)
            return OperationResult<TeacherDto>.Failure("يوجد مستخدم مسجل بهذا البريد الإلكتروني بالفعل");

        if (existing != null && existing.IsDeleted)
            return OperationResult<TeacherDto>.Failure("هذا البريد الإلكتروني مرتبط بحساب محذوف، يرجى استخدام بريد إلكتروني آخر");

        var subjectValidation = await ValidateSubjectIdsAsync(request.SubjectIds);
        if (!subjectValidation.IsSuccess)
            return OperationResult<TeacherDto>.Failure(subjectValidation.Message ?? "المواد المختارة غير صالحة");

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var teacher = _mapper.Map<User>(request);
            teacher.Role = UserRole.Teacher;
            teacher.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            await _unitOfWork.Users.AddAsync(teacher);
            await _unitOfWork.SaveChangesAsync();

            await AddTeacherSubjectsAsync(teacher.Id, subjectValidation.Data!);
            await _unitOfWork.SaveChangesAsync();

            await _unitOfWork.CommitTransactionAsync();

            var dto = _mapper.Map<TeacherDto>(teacher);
            await PopulateTeacherSubjectsAsync(dto);
            return OperationResult<TeacherDto>.Success(dto, "تم إنشاء المعلم بنجاح");
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Users_Email") == true)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return OperationResult<TeacherDto>.Failure("يوجد مستخدم مسجل بهذا البريد الإلكتروني بالفعل");
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<OperationResult<TeacherDto>> UpdateTeacherAsync(UpdateTeacherRequest request)
    {
        var teacher = await _unitOfWork.Users.GetByIdAsync(request.TeacherId);
        if (teacher == null || teacher.IsDeleted || teacher.Role != UserRole.Teacher)
            return OperationResult<TeacherDto>.Failure($"لم يتم العثور على معلم بالمعرف {request.TeacherId}");

        var subjectValidation = await ValidateSubjectIdsAsync(request.SubjectIds);
        if (!subjectValidation.IsSuccess)
            return OperationResult<TeacherDto>.Failure(subjectValidation.Message ?? "المواد المختارة غير صالحة");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            teacher.FullName = request.FullName;
            teacher.Phone = request.Phone ?? teacher.Phone;
            teacher.ProfilePictureUrl = request.ProfilePictureUrl ?? teacher.ProfilePictureUrl;
            teacher.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Users.Update(teacher);
            await _unitOfWork.SaveChangesAsync();

            await SyncTeacherSubjectsAsync(teacher.Id, subjectValidation.Data!);
            await _unitOfWork.SaveChangesAsync();

            await _unitOfWork.CommitTransactionAsync();

            var dto = _mapper.Map<TeacherDto>(teacher);
            await PopulateTeacherSubjectsAsync(dto);
            return OperationResult<TeacherDto>.Success(dto, "تم تحديث بيانات المعلم بنجاح");
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<OperationResult<TeacherDto>> GetTeacherByIdAsync(int id)
    {
        var teacher = await _unitOfWork.Users.GetByIdAsync(id);
        if (teacher == null || teacher.IsDeleted || teacher.Role != UserRole.Teacher)
            return OperationResult<TeacherDto>.Failure($"لم يتم العثور على معلم بالمعرف {id}");

        var dto = _mapper.Map<TeacherDto>(teacher);
        await PopulateTeacherSubjectsAsync(dto);
        return OperationResult<TeacherDto>.Success(dto, "تم استرجاع المعلم بنجاح");
    }

    public async Task<OperationResult<PagedResult<TeacherDto>>> GetAllTeachersAsync(PaginationFilter filter)
    {
        var teachers = await _unitOfWork.Users.FindAsync(u => u.Role == UserRole.Teacher && !u.IsDeleted);
        var ordered = teachers.OrderBy(t => t.FullName).ToList();
        var dtos = _mapper.Map<List<TeacherDto>>(ordered);
        await PopulateTeacherSubjectsAsync(dtos);

        var paged = new PagedResult<TeacherDto>
        {
            Items = dtos,
            TotalCount = dtos.Count,
            Page = filter.Page,
            PageSize = filter.PageSize
        };

        return OperationResult<PagedResult<TeacherDto>>.Success(paged, "تم استرجاع المعلمين بنجاح");
    }

    public async Task<OperationResult> DeleteTeacherAsync(int id)
    {
        var teacher = await _unitOfWork.Users.GetByIdAsync(id);
        if (teacher == null || teacher.IsDeleted || teacher.Role != UserRole.Teacher)
            return OperationResult.Failure($"لم يتم العثور على معلم بالمعرف {id}");

        var activeAssignments = await _unitOfWork.ClassSubjectTeachers.FindAsync(a => a.TeacherId == id && !a.IsDeleted);
        if (activeAssignments.Count > 0)
        {
            teacher.IsActive = false;
            teacher.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Users.Update(teacher);
            await SoftDeleteTeacherSubjectsAsync(id);
            await _unitOfWork.SaveChangesAsync();
            return OperationResult.Success("لدى المعلم تعيينات فعالة، لذلك تم تعطيله بدلا من حذفه");
        }

        teacher.IsDeleted = true;
        teacher.IsActive = false;
        teacher.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(teacher);
        await SoftDeleteTeacherSubjectsAsync(id);
        await _unitOfWork.SaveChangesAsync();
        return OperationResult.Success("تم حذف المعلم بنجاح");
    }

    private static List<int> NormalizeSubjectIds(IEnumerable<int>? subjectIds)
        => subjectIds?
            .Where(id => id > 0)
            .Distinct()
            .ToList()
           ?? new List<int>();

    private async Task<OperationResult<List<int>>> ValidateSubjectIdsAsync(IEnumerable<int> subjectIds)
    {
        var normalized = NormalizeSubjectIds(subjectIds);
        if (normalized.Count == 0)
            return OperationResult<List<int>>.Success(normalized);

        var subjects = await _unitOfWork.Subjects.FindAsync(s => normalized.Contains(s.Id) && !s.IsDeleted);
        if (subjects.Count != normalized.Count)
            return OperationResult<List<int>>.Failure("تم اختيار مادة غير موجودة أو محذوفة");

        return OperationResult<List<int>>.Success(normalized);
    }

    private async Task AddTeacherSubjectsAsync(int teacherId, IReadOnlyCollection<int> subjectIds)
    {
        if (subjectIds.Count == 0)
            return;

        await _unitOfWork.TeacherSubjects.AddRangeAsync(subjectIds.Select(subjectId => new TeacherSubject
        {
            TeacherId = teacherId,
            SubjectId = subjectId
        }));
    }

    private async Task SyncTeacherSubjectsAsync(int teacherId, IReadOnlyCollection<int> subjectIds)
    {
        var existingLinks = await _unitOfWork.TeacherSubjects.GetByTeacherAsync(teacherId);
        var existingSubjectIds = existingLinks.Select(x => x.SubjectId).ToHashSet();

        var linksToDelete = existingLinks
            .Where(x => !subjectIds.Contains(x.SubjectId))
            .ToList();

        if (linksToDelete.Count > 0)
            _unitOfWork.TeacherSubjects.SoftDeleteRange(linksToDelete);

        var subjectIdsToAdd = subjectIds
            .Where(subjectId => !existingSubjectIds.Contains(subjectId))
            .ToList();

        await AddTeacherSubjectsAsync(teacherId, subjectIdsToAdd);
    }

    private async Task SoftDeleteTeacherSubjectsAsync(int teacherId)
    {
        var existingLinks = await _unitOfWork.TeacherSubjects.GetByTeacherAsync(teacherId);
        if (existingLinks.Count > 0)
            _unitOfWork.TeacherSubjects.SoftDeleteRange(existingLinks);
    }

    private async Task PopulateTeacherSubjectsAsync(TeacherDto teacher)
    {
        await PopulateTeacherSubjectsAsync(new[] { teacher });
    }

    private async Task PopulateTeacherSubjectsAsync(IEnumerable<TeacherDto> teachers)
    {
        var teacherDtos = teachers.ToList();
        if (teacherDtos.Count == 0)
            return;

        var teacherIds = teacherDtos.Select(u => u.Id).Distinct().ToList();
        var links = await _unitOfWork.TeacherSubjects.GetByTeacherIdsAsync(teacherIds);
        if (links.Count == 0)
        {
            foreach (var teacher in teacherDtos)
            {
                teacher.SubjectIds = new List<int>();
                teacher.SubjectNames = new List<string>();
            }
            return;
        }

        var subjectIds = links.Select(x => x.SubjectId).Distinct().ToList();
        var subjects = await _unitOfWork.Subjects.FindAsync(s => subjectIds.Contains(s.Id) && !s.IsDeleted);
        var subjectNameMap = subjects.ToDictionary(s => s.Id, s => s.Name);
        var linksByTeacher = links
            .GroupBy(x => x.TeacherId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.SubjectId).Distinct().ToList());

        foreach (var teacher in teacherDtos)
        {
            if (!linksByTeacher.TryGetValue(teacher.Id, out var teacherSubjectIds))
            {
                teacher.SubjectIds = new List<int>();
                teacher.SubjectNames = new List<string>();
                continue;
            }

            teacher.SubjectIds = teacherSubjectIds;
            teacher.SubjectNames = teacherSubjectIds
                .Where(subjectNameMap.ContainsKey)
                .Select(subjectId => subjectNameMap[subjectId])
                .OrderBy(name => name)
                .ToList();
        }
    }
}
