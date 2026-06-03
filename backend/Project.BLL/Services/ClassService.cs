using AutoMapper;
using Common.Results;
using Project.BLL.DTOs;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.Services;

public class ClassService : IClassService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper     _mapper;

    public ClassService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper     = mapper;
    }

    public async Task<OperationResult<ClassDto>> CreateClassAsync(
        CreateClassRequest request)
    {
        // 1. Validate GradeLevel
        var gradeLevel = await _unitOfWork.GradeLevels.GetByIdAsync(request.GradeLevelId);
        if (gradeLevel is null || gradeLevel.IsDeleted)
            return OperationResult<ClassDto>.Failure("الصف الدراسي غير موجود");

        // 2. Validate AcademicYear
        var academicYear = await _unitOfWork.AcademicYears.GetByIdAsync(request.AcademicYearId);
        if (academicYear is null || academicYear.IsDeleted)
            return OperationResult<ClassDto>.Failure("السنة الدراسية غير موجودة");

        // 3. Uniqueness (Name + GradeLevelId + AcademicYearId)
        if (await _unitOfWork.Classes.ExistsByNameGradeLevelAndYearAsync(
                request.Name, request.GradeLevelId, request.AcademicYearId))
            return OperationResult<ClassDto>.Failure(
                "اسم الفصل موجود بالفعل في هذا الصف وهذه السنة الدراسية");

        // 4. Create entity
        var entity = new SchoolClass
        {
            GradeLevelId   = request.GradeLevelId,
            AcademicYearId = request.AcademicYearId,
            Name           = request.Name
        };

        // 5. Persist
        await _unitOfWork.Classes.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        // 6. Reload with navigation properties for mapping (GradeLevel + AcademicYear)
        var withIncludes = await _unitOfWork.Classes.GetByIdWithIncludesAsync(entity.Id);

        return OperationResult<ClassDto>.Success(
            _mapper.Map<ClassDto>(withIncludes),
            "تم إنشاء الفصل بنجاح");
    }

    public async Task<OperationResult<ClassDto>> UpdateClassAsync(
        UpdateClassRequest request)
    {
        // 1. Find entity
        var entity = await _unitOfWork.Classes.GetByIdAsync(request.Id);
        if (entity is null || entity.IsDeleted)
            return OperationResult<ClassDto>.Failure("الفصل غير موجود");

        // 2. Name uniqueness within same (GradeLevelId, AcademicYearId)
        if (request.Name != entity.Name &&
            await _unitOfWork.Classes.ExistsByNameGradeLevelAndYearAsync(
                request.Name, entity.GradeLevelId, entity.AcademicYearId))
            return OperationResult<ClassDto>.Failure("اسم الفصل مستخدم بالفعل");

        // 3. Apply update
        entity.Name      = request.Name;
        entity.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Classes.Update(entity);
        await _unitOfWork.SaveChangesAsync();

        // 4. Reload with navigation properties for mapping
        var withIncludes = await _unitOfWork.Classes.GetByIdWithIncludesAsync(entity.Id);

        return OperationResult<ClassDto>.Success(
            _mapper.Map<ClassDto>(withIncludes),
            "تم تحديث الفصل بنجاح");
    }

    public async Task<OperationResult> DeleteClassAsync(int id)
    {
        // Classes with students, teacher assignments, or timetables are kept for history.
        var entity = await _unitOfWork.Classes.GetByIdAsync(id);
        if (entity is null || entity.IsDeleted)
            return OperationResult.Failure("الفصل غير موجود");

        if (await _unitOfWork.StudentEnrollments.AnyAsync(e => e.ClassId == id) ||
            await _unitOfWork.ClassSubjectTeachers.AnyAsync(cst => cst.ClassId == id) ||
            await _unitOfWork.Timetables.AnyAsync(t => t.ClassId == id))
            return OperationResult.Failure("لا يمكن حذف فصل مستخدم في بيانات أخرى");

        _unitOfWork.Classes.SoftDelete(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("تم حذف الفصل بنجاح");
    }

    public async Task<OperationResult<IEnumerable<ClassDto>>> GetAllClassesAsync(
        GetClassesFilter filter)
    {
        var classes = await _unitOfWork.Classes.GetFilteredWithIncludesAsync(
            filter.AcademicYearId,
            filter.GradeLevelId);

        return OperationResult<IEnumerable<ClassDto>>.Success(
            _mapper.Map<IEnumerable<ClassDto>>(classes),
            "تم جلب الفصول بنجاح");
    }

    public async Task<OperationResult<ClassDto>> GetClassByIdAsync(int id)
    {
        var entity = await _unitOfWork.Classes.GetByIdWithIncludesAsync(id);
        if (entity is null || entity.IsDeleted)
            return OperationResult<ClassDto>.Failure("الفصل غير موجود");

        return OperationResult<ClassDto>.Success(
            _mapper.Map<ClassDto>(entity),
            "تم جلب الفصل بنجاح");
    }

    public async Task<OperationResult<IEnumerable<ClassDto>>> GetClassesByGradeLevelAsync(int gradeLevelId)
    {
        var classes = await _unitOfWork.Classes.FindAsync(c => c.GradeLevelId == gradeLevelId && !c.IsDeleted);
        return OperationResult<IEnumerable<ClassDto>>.Success(
            _mapper.Map<IEnumerable<ClassDto>>(classes),
            "تم جلب الفصول بنجاح");
    }

    public async Task<OperationResult<ClassDto>> GetClassWithStudentsAsync(int classId)
    {
        var entity = await _unitOfWork.Classes.GetByIdWithIncludesAsync(classId);
        if (entity is null || entity.IsDeleted)
            return OperationResult<ClassDto>.Failure("الفصل غير موجود");

        return OperationResult<ClassDto>.Success(
            _mapper.Map<ClassDto>(entity),
            "تم جلب الفصل مع الطلاب بنجاح");
    }

    public async Task<OperationResult<IEnumerable<ClassDto>>> GetClassesByTeacherAsync(int teacherId, int academicYearId)
    {
        var classes = await _unitOfWork.ClassSubjectTeachers.GetClassesForTeacherAsync(teacherId, academicYearId);
        return OperationResult<IEnumerable<ClassDto>>.Success(
            _mapper.Map<IEnumerable<ClassDto>>(classes),
            "تم جلب فصول المعلم بنجاح");
    }

    public async Task<OperationResult<ClassDto>> CreateClassWithStudentsAsync(CreateClassWithStudentsRequest request)
    {
        var academicYear = await _unitOfWork.AcademicYears.GetCurrentAsync();
        if (academicYear is null)
            return OperationResult<ClassDto>.Failure("لا توجد سنة دراسية نشطة");

        var entity = new SchoolClass
        {
            GradeLevelId = 1,
            AcademicYearId = academicYear.Id,
            Name = request.Name.Trim()
        };

        await _unitOfWork.Classes.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        foreach (var studentName in request.Students.Select(s => s.Trim()).Where(s => s.Length > 0))
        {
            var student = (await _unitOfWork.Students.FindAsync(s => s.FullName == studentName)).FirstOrDefault();
            if (student is null)
            {
                student = new Student { FullName = studentName, IsActive = true };
                await _unitOfWork.Students.AddAsync(student);
                await _unitOfWork.SaveChangesAsync();
            }

            var exists = await _unitOfWork.StudentEnrollments.AnyAsync(e =>
                e.StudentId == student.Id &&
                e.ClassId == entity.Id &&
                e.AcademicYearId == academicYear.Id);

            if (!exists)
            {
                await _unitOfWork.StudentEnrollments.AddAsync(new StudentEnrollment
                {
                    StudentId = student.Id,
                    ClassId = entity.Id,
                    AcademicYearId = academicYear.Id,
                    EnrolledAt = DateOnly.FromDateTime(DateTime.UtcNow)
                });
            }
        }

        await _unitOfWork.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(request.Subject) || !string.IsNullOrWhiteSpace(request.Teacher))
        {
            var subject = string.IsNullOrWhiteSpace(request.Subject)
                ? null
                : (await _unitOfWork.Subjects.FindAsync(s => s.Name == request.Subject.Trim())).FirstOrDefault();
            var teacher = string.IsNullOrWhiteSpace(request.Teacher)
                ? null
                : (await _unitOfWork.Users.FindAsync(u => u.FullName == request.Teacher.Trim() && u.Role == UserRole.Teacher)).FirstOrDefault();

            if (subject is not null && teacher is not null)
            {
                var exists = await _unitOfWork.ClassSubjectTeachers.AnyAsync(t =>
                    t.ClassId == entity.Id &&
                    t.SubjectId == subject.Id &&
                    t.TeacherId == teacher.Id &&
                    t.AcademicYearId == academicYear.Id);

                if (!exists)
                {
                    await _unitOfWork.ClassSubjectTeachers.AddAsync(new ClassSubjectTeacher
                    {
                        ClassId = entity.Id,
                        SubjectId = subject.Id,
                        TeacherId = teacher.Id,
                        AcademicYearId = academicYear.Id
                    });
                    await _unitOfWork.SaveChangesAsync();
                }
            }
        }

        var withIncludes = await _unitOfWork.Classes.GetByIdWithIncludesAsync(entity.Id);
        return OperationResult<ClassDto>.Success(
            _mapper.Map<ClassDto>(withIncludes),
            "تم إنشاء الفصل مع الطلاب بنجاح");
    }
}
