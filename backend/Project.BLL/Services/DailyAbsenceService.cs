using AutoMapper;
using Common.Results;
using Project.BLL.DTOs.DailyAbsences;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;

namespace Project.BLL.Services;

public class DailyAbsenceService : IDailyAbsenceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper     _mapper;

    public DailyAbsenceService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper     = mapper;
    }

    public async Task<OperationResult<DailyAbsenceDto>> RecordAbsenceAsync(
        RecordAbsenceRequest request)
    {
        var enrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(request.EnrollmentId);
        if (enrollment is null || enrollment.IsDeleted || enrollment.LeftAt is not null)
            return OperationResult<DailyAbsenceDto>.Failure("القيد غير موجود أو غير نشط");

        if (enrollment.EnrolledAt > request.AbsenceDate)
            return OperationResult<DailyAbsenceDto>.Failure("تاريخ الغياب يسبق تاريخ الالتحاق");

        if (enrollment.LeftAt.HasValue && enrollment.LeftAt < request.AbsenceDate)
            return OperationResult<DailyAbsenceDto>.Failure("تاريخ الغياب بعد تاريخ ترك الفصل");

        var existing = (await _unitOfWork.DailyAbsences.FindAsync(a =>
            a.EnrollmentId == request.EnrollmentId &&
            a.AbsenceDate == request.AbsenceDate &&
            a.ClassSubjectTeacherId == request.ClassSubjectTeacherId &&
            !a.IsDeleted)).FirstOrDefault();

        if (request.ClassSubjectTeacherId.HasValue)
        {
            var cst = await _unitOfWork.ClassSubjectTeachers.GetByIdAsync(request.ClassSubjectTeacherId.Value);
            if (cst is null || cst.IsDeleted)
                return OperationResult<DailyAbsenceDto>.Failure("توزيع المدرس غير موجود");
        }

        if (existing is not null)
        {
            existing.IsAbsent = request.IsAbsent;
            existing.Reason = request.Reason;
            existing.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.DailyAbsences.Update(existing);
            await _unitOfWork.SaveChangesAsync();
            return OperationResult<DailyAbsenceDto>.Success(
                _mapper.Map<DailyAbsenceDto>(existing),
                "تم تحديث الغياب بنجاح");
        }

        var entity = _mapper.Map<DailyAbsence>(request);

        await _unitOfWork.DailyAbsences.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult<DailyAbsenceDto>.Success(
            _mapper.Map<DailyAbsenceDto>(entity),
            "تم تسجيل الغياب بنجاح");
    }

    public async Task<OperationResult<DailyAbsenceDto>> UpdateAbsenceAsync(
        UpdateAbsenceRequest request)
    {
        var entity = await _unitOfWork.DailyAbsences.GetByIdAsync(request.AbsenceId);
        if (entity is null || entity.IsDeleted)
            return OperationResult<DailyAbsenceDto>.Failure("الغياب غير موجود");

        entity.IsAbsent = request.IsAbsent;
        entity.Reason = request.Reason;
        entity.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.DailyAbsences.Update(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult<DailyAbsenceDto>.Success(
            _mapper.Map<DailyAbsenceDto>(entity),
            "تم تحديث الغياب بنجاح");
    }

    public async Task<OperationResult<IEnumerable<DailyAbsenceDto>>> GetAbsencesByEnrollmentAsync(
        GetAbsenceFilter filter)
    {
        var enrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(filter.EnrollmentId);
        if (enrollment is null || enrollment.IsDeleted)
            return OperationResult<IEnumerable<DailyAbsenceDto>>.Failure("القيد غير موجود");

        IReadOnlyList<DailyAbsence> absences;

        if (filter.FromDate.HasValue && filter.ToDate.HasValue)
        {
            if (filter.FromDate >= filter.ToDate)
                return OperationResult<IEnumerable<DailyAbsenceDto>>.Failure("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

            absences = await _unitOfWork.DailyAbsences.GetByEnrollmentAndDateRangeAsync(
                filter.EnrollmentId, filter.FromDate.Value, filter.ToDate.Value);
        }
        else
        {
            absences = await _unitOfWork.DailyAbsences.GetByEnrollmentIdAsync(filter.EnrollmentId);
        }

        if (filter.ClassSubjectTeacherId.HasValue)
            absences = absences.Where(a => a.ClassSubjectTeacherId == filter.ClassSubjectTeacherId).ToList();

        absences = absences.Where(a => a.IsAbsent).OrderByDescending(a => a.AbsenceDate).ToList();

        return OperationResult<IEnumerable<DailyAbsenceDto>>.Success(
            _mapper.Map<IEnumerable<DailyAbsenceDto>>(absences),
            "تم جلب سجل الغياب بنجاح");
    }

    public async Task<OperationResult> DeleteAbsenceAsync(int id)
    {
        var entity = await _unitOfWork.DailyAbsences.GetByIdAsync(id);
        if (entity is null || entity.IsDeleted)
            return OperationResult.Failure("الغياب غير موجود");

        _unitOfWork.DailyAbsences.SoftDelete(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("تم حذف الغياب بنجاح");
    }

    public async Task<OperationResult<IEnumerable<DailyAbsenceDto>>> GetAbsencesByEnrollmentsAsync(
        List<int> enrollmentIds, DateOnly fromDate, DateOnly toDate)
    {
        if (enrollmentIds == null || enrollmentIds.Count == 0)
            return OperationResult<IEnumerable<DailyAbsenceDto>>.Failure("معرفات القيد مطلوبة");

        if (fromDate > toDate)
            return OperationResult<IEnumerable<DailyAbsenceDto>>.Failure("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

        var absences = await _unitOfWork.DailyAbsences.GetByEnrollmentsAndDateRangeAsync(
            enrollmentIds, fromDate, toDate);

        return OperationResult<IEnumerable<DailyAbsenceDto>>.Success(
            _mapper.Map<IEnumerable<DailyAbsenceDto>>(absences),
            "تم جلب سجل الغياب بنجاح");
    }

    public async Task<OperationResult<AbsenceSummaryDto>> GetAbsenceSummaryAsync(
        int enrollmentId, int? classSubjectTeacherId = null)
    {
        var enrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(enrollmentId);
        if (enrollment is null || enrollment.IsDeleted)
            return OperationResult<AbsenceSummaryDto>.Failure("القيد غير موجود");

        var totalAbsences = await _unitOfWork.DailyAbsences.GetAbsenceCountAsync(
            enrollmentId, classSubjectTeacherId);

        var absenceDates = await _unitOfWork.DailyAbsences.GetAbsenceDatesAsync(
            enrollmentId, classSubjectTeacherId);

        var result = new AbsenceSummaryDto
        {
            TotalAbsences = totalAbsences,
            AbsenceDates = absenceDates,
            PerSubjectBreakdown = new List<SubjectAbsenceBreakdown>()
        };

        return OperationResult<AbsenceSummaryDto>.Success(
            result,
            "تم جلب ملخص الغياب بنجاح");
    }

    public async Task<OperationResult<DailyAbsenceDto>> GetAbsenceByIdAsync(int id)
    {
        var entity = await _unitOfWork.DailyAbsences.GetByIdAsync(id);
        if (entity is null || entity.IsDeleted)
            return OperationResult<DailyAbsenceDto>.Failure("الغياب غير موجود");

        return OperationResult<DailyAbsenceDto>.Success(
            _mapper.Map<DailyAbsenceDto>(entity),
            "تم جلب الغياب بنجاح");
    }

    public async Task<OperationResult<IEnumerable<DailyAbsenceDto>>> GetAbsencesByClassAsync(int classId, DateOnly date)
    {
        var classEntity = await _unitOfWork.Classes.GetByIdAsync(classId);
        if (classEntity is null || classEntity.IsDeleted)
            return OperationResult<IEnumerable<DailyAbsenceDto>>.Failure("الفصل غير موجود");

        var cstList = await _unitOfWork.ClassSubjectTeachers
            .FindAsync(cst => cst.ClassId == classId && !cst.IsDeleted);

        if (!cstList.Any())
            return OperationResult<IEnumerable<DailyAbsenceDto>>.Success(
                new List<DailyAbsenceDto>(), "لا توجد مواد لهذا الفصل");

        var allAbsences = new List<DailyAbsence>();
        foreach (var cst in cstList)
        {
            var absences = await _unitOfWork.DailyAbsences
                .GetByClassSubjectTeacherAndDateAsync(cst.Id, date);
            allAbsences.AddRange(absences);
        }

        return OperationResult<IEnumerable<DailyAbsenceDto>>.Success(
            _mapper.Map<IEnumerable<DailyAbsenceDto>>(allAbsences),
            "تم جلب غياب الفصل بنجاح");
    }

    public async Task<OperationResult<IEnumerable<DailyAbsenceDto>>> GetAbsencesByDateRangeAsync(
        DateOnly fromDate, DateOnly toDate, int? classSubjectTeacherId = null)
    {
        if (fromDate > toDate)
            return OperationResult<IEnumerable<DailyAbsenceDto>>.Failure("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

        IReadOnlyList<DailyAbsence> absences;

        if (classSubjectTeacherId.HasValue)
        {
            absences = await _unitOfWork.DailyAbsences
                .GetByClassSubjectTeacherAndDateRangeAsync(classSubjectTeacherId.Value, fromDate, toDate);
        }
        else
        {
            absences = await _unitOfWork.DailyAbsences
                .FindAsync(a => a.AbsenceDate >= fromDate && a.AbsenceDate <= toDate && a.IsAbsent && !a.IsDeleted);
        }

        return OperationResult<IEnumerable<DailyAbsenceDto>>.Success(
            _mapper.Map<IEnumerable<DailyAbsenceDto>>(absences),
            "تم جلب سجل الغياب في المدى التاريخي بنجاح");
    }
}
