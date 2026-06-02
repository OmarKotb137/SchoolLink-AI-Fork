using AutoMapper;
using Common.Results;
using Project.BLL.DTOs;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using SchoolLink.Domain.Entities;

namespace Project.BLL.Services;

public class SubjectService : ISubjectService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper     _mapper;

    public SubjectService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper     = mapper;
    }

    public async Task<OperationResult<SubjectDto>> CreateSubjectAsync(
        CreateSubjectRequest request)
    {
        // 1. Name uniqueness
        if (await _unitOfWork.Subjects.ExistsByNameAsync(request.Name))
            return OperationResult<SubjectDto>.Failure("اسم المادة موجود بالفعل");

        // 2. Code uniqueness (if provided)
        if (request.Code is not null &&
            await _unitOfWork.Subjects.ExistsByCodeAsync(request.Code))
            return OperationResult<SubjectDto>.Failure("كود المادة موجود بالفعل");

        // 3. Create
        var entity = _mapper.Map<Subject>(request);
        await _unitOfWork.Subjects.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult<SubjectDto>.Success(
            _mapper.Map<SubjectDto>(entity),
            "تم إنشاء المادة بنجاح");
    }

    public async Task<OperationResult<SubjectDto>> UpdateSubjectAsync(
        UpdateSubjectRequest request)
    {
        // 1. Find entity
        var entity = await _unitOfWork.Subjects.GetByIdAsync(request.Id);
        if (entity is null || entity.IsDeleted)
            return OperationResult<SubjectDto>.Failure("المادة غير موجودة");

        // 2. Name uniqueness (excluding self)
        var byName = await _unitOfWork.Subjects.GetByNameAsync(request.Name);
        if (byName is not null && !byName.IsDeleted && byName.Id != request.Id)
            return OperationResult<SubjectDto>.Failure("اسم المادة مستخدم بالفعل");

        // 3. Code uniqueness (excluding self, only if code provided)
        if (request.Code is not null)
        {
            var byCode = await _unitOfWork.Subjects.GetByCodeAsync(request.Code);
            if (byCode is not null && !byCode.IsDeleted && byCode.Id != request.Id)
                return OperationResult<SubjectDto>.Failure("كود المادة مستخدم بالفعل");
        }

        // 4. Apply updates
        entity.Name      = request.Name;
        entity.Code      = request.Code;
        entity.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Subjects.Update(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult<SubjectDto>.Success(
            _mapper.Map<SubjectDto>(entity),
            "تم تحديث المادة بنجاح");
    }

    public async Task<OperationResult> DeleteSubjectAsync(int id)
    {
        // Subjects are protected if they are already used in teaching or evaluation setup.
        var entity = await _unitOfWork.Subjects.GetByIdAsync(id);
        if (entity is null || entity.IsDeleted)
            return OperationResult.Failure("المادة غير موجودة");

        if (await _unitOfWork.ClassSubjectTeachers.AnyAsync(cst => cst.SubjectId == id) ||
            await _unitOfWork.EvaluationTemplates.AnyAsync(t => t.SubjectId == id))
            return OperationResult.Failure("لا يمكن حذف مادة مستخدمة في بيانات أخرى");

        _unitOfWork.Subjects.SoftDelete(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("تم حذف المادة بنجاح");
    }

    public async Task<OperationResult<SubjectDto>> GetSubjectByIdAsync(int id)
    {
        var entity = await _unitOfWork.Subjects.GetByIdAsync(id);
        if (entity is null || entity.IsDeleted)
            return OperationResult<SubjectDto>.Failure("المادة غير موجودة");

        return OperationResult<SubjectDto>.Success(
            _mapper.Map<SubjectDto>(entity),
            "تم جلب المادة بنجاح");
    }

    public async Task<OperationResult<IEnumerable<SubjectDto>>> GetAllSubjectsAsync()
    {
        var all  = await _unitOfWork.Subjects.GetAllAsync();
        var list = all.Where(s => !s.IsDeleted).OrderBy(s => s.Name);
        return OperationResult<IEnumerable<SubjectDto>>.Success(
            _mapper.Map<IEnumerable<SubjectDto>>(list),
            "تم جلب المواد بنجاح");
    }
}
