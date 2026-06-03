using AutoMapper;
using Common.Results;
using Project.BLL.DTOs.Exam;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.Services
{
    public class ExamService : IExamService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ExamService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<OperationResult<List<ExamSummaryDto>>> GetAllByClassSubjectTeacherAsync(int classSubjectTeacherId)
        {
            var exams = await _unitOfWork.Exams
                .GetByClassSubjectTeacherIdAsync(classSubjectTeacherId, CancellationToken.None);

            var dtos = _mapper.Map<List<ExamSummaryDto>>(exams);
            return OperationResult<List<ExamSummaryDto>>.Success(dtos);
        }

        public async Task<OperationResult<GetExamDto>> GetByIdAsync(int id)
        {
            var exam = await _unitOfWork.Exams.GetWithQuestionsAsync(id, CancellationToken.None);

            if (exam == null || exam.IsDeleted)
                return OperationResult<GetExamDto>.Failure("الامتحان غير موجود", 404);

            var dto = _mapper.Map<GetExamDto>(exam);
            return OperationResult<GetExamDto>.Success(dto);
        }

        public async Task<OperationResult<ExamSummaryDto>> CreateAsync(CreateExamDto dto)
        {
            var classSubjectTeacher = await _unitOfWork.ClassSubjectTeachers
                .GetByIdAsync(dto.ClassSubjectTeacherId);

            if (classSubjectTeacher == null || classSubjectTeacher.IsDeleted)
                return OperationResult<ExamSummaryDto>.Failure("المادة غير موجودة", 404);

            var exam = _mapper.Map<Exam>(dto);

            await _unitOfWork.Exams.AddAsync(exam);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);

            var resultDto = _mapper.Map<ExamSummaryDto>(exam);
            return OperationResult<ExamSummaryDto>.Success(resultDto, "تم إنشاء الامتحان بنجاح");
        }

        public async Task<OperationResult<ExamSummaryDto>> UpdateAsync(UpdateExamDto dto)
        {
            var exam = await _unitOfWork.Exams.GetByIdAsync(dto.Id);

            if (exam == null || exam.IsDeleted)
                return OperationResult<ExamSummaryDto>.Failure("الامتحان غير موجود", 404);

            exam.Title = dto.Title;
            exam.StartTime = dto.StartTime;
            exam.EndTime = dto.EndTime;
            exam.DurationMinutes = dto.DurationMinutes;
            exam.TotalScore = dto.TotalScore;
            exam.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Exams.Update(exam);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);

            var resultDto = _mapper.Map<ExamSummaryDto>(exam);
            return OperationResult<ExamSummaryDto>.Success(resultDto, "تم تحديث الامتحان بنجاح");
        }

        public async Task<OperationResult> DeleteAsync(int id)
        {
            var exam = await _unitOfWork.Exams.GetByIdAsync(id);

            if (exam == null || exam.IsDeleted)
                return OperationResult.Failure("الامتحان غير موجود");

            _unitOfWork.Exams.SoftDelete(exam);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);

            return OperationResult.Success("تم حذف الامتحان بنجاح");
        }

        public async Task<OperationResult> PublishAsync(int id)
        {
            var exam = await _unitOfWork.Exams.GetByIdAsync(id);

            if (exam == null || exam.IsDeleted)
                return OperationResult.Failure("الامتحان غير موجود");

            if (exam.IsPublished)
                return OperationResult.Failure("الامتحان منشور بالفعل");

            exam.IsPublished = true;
            exam.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Exams.Update(exam);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);

            return OperationResult.Success("تم نشر الامتحان بنجاح");
        }

    public async Task<OperationResult> UnPublishAsync(int id)
    {
        var exam = await _unitOfWork.Exams.GetByIdAsync(id);

            if (exam == null || exam.IsDeleted)
                return OperationResult.Failure("الامتحان غير موجود");

            if (!exam.IsPublished)
                return OperationResult.Failure("الامتحان غير منشور");

        exam.IsPublished = false;
        exam.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Exams.Update(exam);
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        return OperationResult.Success("تم إلغاء نشر الامتحان بنجاح");
    }

    public async Task<OperationResult<List<ExamSummaryDto>>> GetExamsByStudentAsync(int enrollmentId)
    {
        var enrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(enrollmentId);
        if (enrollment == null || enrollment.IsDeleted)
            return OperationResult<List<ExamSummaryDto>>.Failure("التسجيل غير موجود", 404);

        var csts = await _unitOfWork.ClassSubjectTeachers
            .GetByClassAndYearAsync(enrollment.ClassId, enrollment.AcademicYearId);

        var cstIds = csts.Select(c => c.Id).ToList();
        if (cstIds.Count == 0)
            return OperationResult<List<ExamSummaryDto>>.Success(new List<ExamSummaryDto>());

        var allExams = await _unitOfWork.Exams
            .FindAsync(e => cstIds.Contains(e.ClassSubjectTeacherId) && !e.IsDeleted);

        var dtos = _mapper.Map<List<ExamSummaryDto>>(allExams);
        return OperationResult<List<ExamSummaryDto>>.Success(dtos, "تم جلب الامتحانات بنجاح");
    }

    public async Task<OperationResult<List<ExamSummaryDto>>> GetUpcomingExamsAsync(int classId, int academicYearId)
    {
        var classEntity = await _unitOfWork.Classes.GetByIdAsync(classId);
        if (classEntity == null || classEntity.IsDeleted)
            return OperationResult<List<ExamSummaryDto>>.Failure("الفصل غير موجود", 404);

        var exams = await _unitOfWork.Exams.GetUpcomingByClassAsync(classId, 7);
        var dtos = _mapper.Map<List<ExamSummaryDto>>(exams.Where(e => !e.IsDeleted));
        return OperationResult<List<ExamSummaryDto>>.Success(dtos, "تم جلب الامتحانات القادمة بنجاح");
    }
}
}