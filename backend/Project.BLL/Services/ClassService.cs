using AutoMapper;
using Common.Results;
using Project.BLL.DTOs;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using   Project.Domain.Entities;

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
}
