using AutoMapper;
using Common.Results;
using Project.BLL.DTOs.Exam;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Entities;

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
                return OperationResult<GetExamDto>.Failure("Exam not found", 404);

            var dto = _mapper.Map<GetExamDto>(exam);
            return OperationResult<GetExamDto>.Success(dto);
        }

        public async Task<OperationResult<ExamSummaryDto>> CreateAsync(CreateExamDto dto)
        {
            var classSubjectTeacher = await _unitOfWork.ClassSubjectTeachers
                .GetByIdAsync(dto.ClassSubjectTeacherId);

            if (classSubjectTeacher == null || classSubjectTeacher.IsDeleted)
                return OperationResult<ExamSummaryDto>.Failure("ClassSubjectTeacher not found", 404);

            var exam = _mapper.Map<Exam>(dto);

            await _unitOfWork.Exams.AddAsync(exam);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);

            var resultDto = _mapper.Map<ExamSummaryDto>(exam);
            return OperationResult<ExamSummaryDto>.Success(resultDto, "Exam created successfully");
        }

        public async Task<OperationResult<ExamSummaryDto>> UpdateAsync(UpdateExamDto dto)
        {
            var exam = await _unitOfWork.Exams.GetByIdAsync(dto.Id);

            if (exam == null || exam.IsDeleted)
                return OperationResult<ExamSummaryDto>.Failure("Exam not found", 404);

            exam.Title = dto.Title;
            exam.StartTime = dto.StartTime;
            exam.EndTime = dto.EndTime;
            exam.DurationMinutes = dto.DurationMinutes;
            exam.TotalScore = dto.TotalScore;
            exam.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Exams.Update(exam);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);

            var resultDto = _mapper.Map<ExamSummaryDto>(exam);
            return OperationResult<ExamSummaryDto>.Success(resultDto, "Exam updated successfully");
        }

        public async Task<OperationResult> DeleteAsync(int id)
        {
            var exam = await _unitOfWork.Exams.GetByIdAsync(id);

            if (exam == null || exam.IsDeleted)
                return OperationResult.Failure("Exam not found");

            _unitOfWork.Exams.SoftDelete(exam);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);

            return OperationResult.Success("Exam deleted successfully");
        }

        public async Task<OperationResult> PublishAsync(int id)
        {
            var exam = await _unitOfWork.Exams.GetByIdAsync(id);

            if (exam == null || exam.IsDeleted)
                return OperationResult.Failure("Exam not found");

            if (exam.IsPublished)
                return OperationResult.Failure("Exam is already published");

            exam.IsPublished = true;
            exam.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Exams.Update(exam);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);

            return OperationResult.Success("Exam published successfully");
        }

        public async Task<OperationResult> UnPublishAsync(int id)
        {
            var exam = await _unitOfWork.Exams.GetByIdAsync(id);

            if (exam == null || exam.IsDeleted)
                return OperationResult.Failure("Exam not found");

            if (!exam.IsPublished)
                return OperationResult.Failure("Exam is not published");

            exam.IsPublished = false;
            exam.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Exams.Update(exam);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);

            return OperationResult.Success("Exam unpublished successfully");
        }
    }
}