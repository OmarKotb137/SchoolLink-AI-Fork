using AutoMapper;
using Common.Results;
using Project.BLL.DTOs;
using Project.BLL.DTOs.Users;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.Services;

public class ClassSubjectTeacherService : IClassSubjectTeacherService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper     _mapper;

    public ClassSubjectTeacherService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper     = mapper;
    }

    public async Task<OperationResult<ClassSubjectTeacherDto>> AssignTeacherAsync(
        AssignTeacherRequest request)
    {
        // 1. Validate Class
        var schoolClass = await _unitOfWork.Classes.GetByIdAsync(request.ClassId);
        if (schoolClass is null || schoolClass.IsDeleted)
            return OperationResult<ClassSubjectTeacherDto>.Failure("الفصل غير موجود");

        // 2. Validate Subject
        var subject = await _unitOfWork.Subjects.GetByIdAsync(request.SubjectId);
        if (subject is null || subject.IsDeleted)
            return OperationResult<ClassSubjectTeacherDto>.Failure("المادة غير موجودة");

        // 3. Validate Teacher existence + Role
        var teacher = await _unitOfWork.Users.GetByIdAsync(request.TeacherId);
        if (teacher is null || teacher.IsDeleted)
            return OperationResult<ClassSubjectTeacherDto>.Failure("المعلم غير موجود");
        if (teacher.Role != UserRole.Teacher)
            return OperationResult<ClassSubjectTeacherDto>.Failure("المستخدم ليس معلما");

        // 4. Validate AcademicYear
        var academicYear = await _unitOfWork.AcademicYears.GetByIdAsync(request.AcademicYearId);
        if (academicYear is null || academicYear.IsDeleted)
            return OperationResult<ClassSubjectTeacherDto>.Failure("السنة الدراسية غير موجودة");

        // 5. Uniqueness: one teacher per subject per class per year
        if (await _unitOfWork.ClassSubjectTeachers.ExistsByClassSubjectAndYearAsync(
                request.ClassId, request.SubjectId, request.AcademicYearId))
            return OperationResult<ClassSubjectTeacherDto>.Failure(
                "يوجد معلم معين بالفعل لهذه المادة في هذا الفصل وهذه السنة الدراسية");

        // 6. Create entity
        var entity = new ClassSubjectTeacher
        {
            ClassId        = request.ClassId,
            SubjectId      = request.SubjectId,
            TeacherId      = request.TeacherId,
            AcademicYearId = request.AcademicYearId,
            WeeklyPeriods   = request.WeeklyPeriods
        };

        // 7. Persist
        await _unitOfWork.ClassSubjectTeachers.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        // 8. Reload with all navigation properties, including AcademicYear
        var withDetails = await _unitOfWork.ClassSubjectTeachers
            .GetWithAllDetailsAsync(entity.Id);

        return OperationResult<ClassSubjectTeacherDto>.Success(
            _mapper.Map<ClassSubjectTeacherDto>(withDetails),
            "تم تعيين المعلم بنجاح");
    }

    public async Task<OperationResult<ClassSubjectTeacherDto>> GetAssignmentByIdAsync(int id)
    {
        var assignment = await _unitOfWork.ClassSubjectTeachers.GetWithAllDetailsAsync(id);
        if (assignment is null || assignment.IsDeleted)
            return OperationResult<ClassSubjectTeacherDto>.Failure("تعيين المعلم غير موجود");

        return OperationResult<ClassSubjectTeacherDto>.Success(
            _mapper.Map<ClassSubjectTeacherDto>(assignment),
            "تم جلب تعيين المعلم بنجاح");
    }

    public async Task<OperationResult<ClassSubjectTeacherDto>> UpdateTeacherAssignmentAsync(
        UpdateTeacherAssignmentRequest request)
    {
        var assignment = await _unitOfWork.ClassSubjectTeachers.GetByIdAsync(request.AssignmentId);
        if (assignment is null || assignment.IsDeleted)
            return OperationResult<ClassSubjectTeacherDto>.Failure("تعيين المعلم غير موجود");

        var teacher = await _unitOfWork.Users.GetByIdAsync(request.TeacherId);
        if (teacher is null || teacher.IsDeleted)
            return OperationResult<ClassSubjectTeacherDto>.Failure("المعلم غير موجود");
        if (teacher.Role != UserRole.Teacher)
            return OperationResult<ClassSubjectTeacherDto>.Failure("المستخدم ليس معلما");

        if (assignment.TeacherId == request.TeacherId)
        {
            var current = await _unitOfWork.ClassSubjectTeachers.GetWithAllDetailsAsync(assignment.Id);
            return OperationResult<ClassSubjectTeacherDto>.Success(
                _mapper.Map<ClassSubjectTeacherDto>(current),
                "المعلم مربوط بالفعل بهذا التعيين");
        }

        assignment.TeacherId = request.TeacherId;
        assignment.WeeklyPeriods = request.WeeklyPeriods;
        assignment.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.ClassSubjectTeachers.Update(assignment);
        await _unitOfWork.SaveChangesAsync();

        var withDetails = await _unitOfWork.ClassSubjectTeachers.GetWithAllDetailsAsync(assignment.Id);

        return OperationResult<ClassSubjectTeacherDto>.Success(
            _mapper.Map<ClassSubjectTeacherDto>(withDetails),
            "تم تحديث تعيين المعلم بنجاح");
    }

    public async Task<OperationResult> UnassignTeacherAsync(int id)
    {
        var assignment = await _unitOfWork.ClassSubjectTeachers.GetByIdAsync(id);
        if (assignment is null || assignment.IsDeleted)
            return OperationResult.Failure("تعيين المعلم غير موجود");

        _unitOfWork.ClassSubjectTeachers.SoftDelete(assignment);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("تم إلغاء تعيين المعلم بنجاح");
    }

    public async Task<OperationResult<IEnumerable<ClassSubjectTeacherDto>>> GetByClassAsync(
        int classId, int academicYearId)
    {
        var schoolClass = await _unitOfWork.Classes.GetByIdAsync(classId);
        if (schoolClass is null || schoolClass.IsDeleted)
            return OperationResult<IEnumerable<ClassSubjectTeacherDto>>.Failure(
                "الفصل غير موجود");

        var list = await _unitOfWork.ClassSubjectTeachers
            .GetByClassWithAllDetailsAsync(classId, academicYearId);

        return OperationResult<IEnumerable<ClassSubjectTeacherDto>>.Success(
            _mapper.Map<IEnumerable<ClassSubjectTeacherDto>>(list),
            "تم جلب تعيينات الفصل بنجاح");
    }

    public async Task<OperationResult<IEnumerable<UserDto>>> GetAvailableTeachersForSubjectAsync(int subjectId, int classId, int academicYearId)
    {
        var allTeachers = await _unitOfWork.Users.GetByRoleAsync(UserRole.Teacher);
        var assigned = await _unitOfWork.ClassSubjectTeachers
            .GetBySubjectAndYearAsync(subjectId, academicYearId);

        var assignedIds = assigned
            .Where(cst => cst.ClassId == classId && !cst.IsDeleted)
            .Select(cst => cst.TeacherId)
            .ToHashSet();

        var available = allTeachers.Where(t => !t.IsDeleted && !assignedIds.Contains(t.Id)).ToList();
        var dtos = _mapper.Map<IEnumerable<UserDto>>(available);
        return OperationResult<IEnumerable<UserDto>>.Success(dtos, "تم جلب المعلمين المتاحين بنجاح");
    }

    public async Task<OperationResult> BulkAssignTeachersAsync(List<AssignTeacherRequest> requests)
    {
        foreach (var req in requests)
        {
            var schoolClass = await _unitOfWork.Classes.GetByIdAsync(req.ClassId);
            if (schoolClass is null || schoolClass.IsDeleted)
                return OperationResult.Failure($"الفصل {req.ClassId} غير موجود");

            var subject = await _unitOfWork.Subjects.GetByIdAsync(req.SubjectId);
            if (subject is null || subject.IsDeleted)
                return OperationResult.Failure($"المادة {req.SubjectId} غير موجودة");

            var teacher = await _unitOfWork.Users.GetByIdAsync(req.TeacherId);
            if (teacher is null || teacher.IsDeleted || teacher.Role != UserRole.Teacher)
                return OperationResult.Failure($"المعلم {req.TeacherId} غير موجود أو ليس معلماً");

            if (await _unitOfWork.ClassSubjectTeachers.ExistsByClassSubjectAndYearAsync(
                    req.ClassId, req.SubjectId, req.AcademicYearId))
                return OperationResult.Failure($"تعيين مكرر للفصل {req.ClassId} والمادة {req.SubjectId}");

            var entity = new ClassSubjectTeacher
            {
                ClassId = req.ClassId,
                SubjectId = req.SubjectId,
                TeacherId = req.TeacherId,
                AcademicYearId = req.AcademicYearId,
                WeeklyPeriods = req.WeeklyPeriods
            };

            await _unitOfWork.ClassSubjectTeachers.AddAsync(entity);
        }

        await _unitOfWork.SaveChangesAsync();
        return OperationResult.Success("تم تعيين المعلمين بنجاح");
    }

    public async Task<OperationResult<IEnumerable<ClassSubjectTeacherDto>>> GetByTeacherAsync(
        int teacherId, int academicYearId)
    {
        var teacher = await _unitOfWork.Users.GetByIdAsync(teacherId);
        if (teacher is null || teacher.IsDeleted)
            return OperationResult<IEnumerable<ClassSubjectTeacherDto>>.Failure(
                "المعلم غير موجود");
        if (teacher.Role != UserRole.Teacher)
            return OperationResult<IEnumerable<ClassSubjectTeacherDto>>.Failure(
                "المستخدم ليس معلما");

        var list = await _unitOfWork.ClassSubjectTeachers
            .GetByTeacherWithAllDetailsAsync(teacherId, academicYearId);

        return OperationResult<IEnumerable<ClassSubjectTeacherDto>>.Success(
            _mapper.Map<IEnumerable<ClassSubjectTeacherDto>>(list),
            "تم جلب تعيينات المعلم بنجاح");
    }
}
