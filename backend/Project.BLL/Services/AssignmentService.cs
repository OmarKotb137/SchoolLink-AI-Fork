using AutoMapper;
using Common.Results;
using Project.BLL.DTOs.Assignment;
using Project.BLL.DTOs.AssignmentQuestion;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.Services
{
    public class AssignmentService : IAssignmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public AssignmentService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<OperationResult<List<AssignmentDto>>> GetAllByClassSubjectTeacherAsync(int classSubjectTeacherId)
        {
            var assignments = await _unitOfWork.Assignments
                .GetByClassSubjectTeacherIdAsync(classSubjectTeacherId);

            var dtos = _mapper.Map<List<AssignmentDto>>(assignments);
            return OperationResult<List<AssignmentDto>>.Success(dtos);
        }

        public async Task<OperationResult<GetAssignmentDto>> GetByIdAsync(int id)
        {
            var assignment = await _unitOfWork.Assignments.GetWithQuestionsAsync(id);

            if (assignment == null || assignment.IsDeleted)
                return OperationResult<GetAssignmentDto>.Failure("الواجب غير موجود", 404);

            var dto = _mapper.Map<GetAssignmentDto>(assignment);
            return OperationResult<GetAssignmentDto>.Success(dto);
        }

        public async Task<OperationResult<AssignmentDto>> CreateAsync(CreateAssignmentDto dto)
        {
            var classSubjectTeacher = await _unitOfWork.ClassSubjectTeachers
                .GetByIdAsync(dto.ClassSubjectTeacherId);

            if (classSubjectTeacher == null || classSubjectTeacher.IsDeleted)
                return OperationResult<AssignmentDto>.Failure("بيان الفصل-المادة-المعلم غير موجود", 404);

            var assignment = _mapper.Map<Assignment>(dto);

            await _unitOfWork.Assignments.AddAsync(assignment);
            await _unitOfWork.SaveChangesAsync();

            var resultDto = _mapper.Map<AssignmentDto>(assignment);
            return OperationResult<AssignmentDto>.Success(resultDto);
        }

        public async Task<OperationResult<AssignmentDto>> UpdateAsync(UpdateAssignmentDto dto)
        {
            var assignment = await _unitOfWork.Assignments.GetByIdAsync(dto.Id);

            if (assignment == null || assignment.IsDeleted)
                return OperationResult<AssignmentDto>.Failure("الواجب غير موجود", 404);

            assignment.Title = dto.Title;
            assignment.Description = dto.Description;
            assignment.DueDate = dto.DueDate;
            assignment.MaxScore = dto.MaxScore;
            assignment.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Assignments.Update(assignment);
            await _unitOfWork.SaveChangesAsync();

            var resultDto = _mapper.Map<AssignmentDto>(assignment);
            return OperationResult<AssignmentDto>.Success(resultDto);
        }

        public async Task<OperationResult> DeleteAsync(int id)
        {
            var assignment = await _unitOfWork.Assignments.GetByIdAsync(id);

            if (assignment == null || assignment.IsDeleted)
                return OperationResult.Failure("الواجب غير موجود");

            _unitOfWork.Assignments.SoftDelete(assignment);
            await _unitOfWork.SaveChangesAsync();

            return OperationResult.Success("تم حذف الواجب بنجاح");
        }

        public async Task<OperationResult<AssignmentDto>> AddQuestionAsync(CreateAssignmentQuestionDto dto)
        {
            var assignment = await _unitOfWork.Assignments.GetWithQuestionsAsync(dto.AssignmentId);

            if (assignment == null || assignment.IsDeleted)
                return OperationResult<AssignmentDto>.Failure("الواجب غير موجود", 404);

            var question = _mapper.Map<AssignmentQuestion>(dto);
            assignment.Questions.Add(question);

            _unitOfWork.Assignments.Update(assignment);
            await _unitOfWork.SaveChangesAsync();

            var resultDto = _mapper.Map<AssignmentDto>(assignment);
            return OperationResult<AssignmentDto>.Success(resultDto);
        }

        public async Task<OperationResult> PublishAsync(int id)
        {
            var assignment = await _unitOfWork.Assignments.GetByIdAsync(id);
            if (assignment == null || assignment.IsDeleted)
                return OperationResult.Failure("الواجب غير موجود", 404);

            if (assignment.IsPublished)
                return OperationResult.Failure("الواجب منشور بالفعل");

            assignment.IsPublished = true;
            assignment.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Assignments.Update(assignment);
            await _unitOfWork.SaveChangesAsync();

            return OperationResult.Success("تم نشر الواجب بنجاح");
        }

        public async Task<OperationResult> UnPublishAsync(int id)
        {
            var assignment = await _unitOfWork.Assignments.GetByIdAsync(id);
            if (assignment == null || assignment.IsDeleted)
                return OperationResult.Failure("الواجب غير موجود", 404);

            if (!assignment.IsPublished)
                return OperationResult.Failure("الواجب غير منشور");

            assignment.IsPublished = false;
            assignment.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Assignments.Update(assignment);
            await _unitOfWork.SaveChangesAsync();

            return OperationResult.Success("تم إلغاء نشر الواجب بنجاح");
        }

        public async Task<OperationResult> UpdateQuestionAsync(UpdateAssignmentQuestionDto dto)
        {
            var question = await _unitOfWork.AssignmentQuestions.GetByIdAsync(dto.Id);

            if (question == null || question.IsDeleted)
                return OperationResult.Failure("السؤال غير موجود");

            question.QuestionText = dto.QuestionText;
            question.QuestionType = dto.QuestionType;
            question.ImageUrl = dto.ImageUrl;
            question.CorrectAnswer = dto.CorrectAnswer;
            question.DisplayOrder = dto.DisplayOrder;
            question.Points = dto.Points;
            question.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.AssignmentQuestions.Update(question);
            await _unitOfWork.SaveChangesAsync();

            return OperationResult.Success("تم تعديل السؤال بنجاح");
        }

        public async Task<OperationResult> DeleteQuestionAsync(int questionId)
        {
            var question = await _unitOfWork.AssignmentQuestions.GetByIdAsync(questionId);

            if (question == null || question.IsDeleted)
                return OperationResult.Failure("السؤال غير موجود");

            _unitOfWork.AssignmentQuestions.SoftDelete(question);
            await _unitOfWork.SaveChangesAsync();

            return OperationResult.Success("تم حذف السؤال بنجاح");
        }

    public async Task<OperationResult<List<AssignmentDto>>> GetByTeacherAsync(int teacherId, int academicYearId)
    {
        var teacher = await _unitOfWork.Users.GetByIdAsync(teacherId);
        if (teacher == null || teacher.IsDeleted || teacher.Role != UserRole.Teacher)
            return OperationResult<List<AssignmentDto>>.Failure("المعلم غير موجود", 404);

        var csts = await _unitOfWork.ClassSubjectTeachers
            .GetByTeacherAndYearAsync(teacherId, academicYearId);

        var cstIds = csts.Select(c => c.Id).ToList();
        if (cstIds.Count == 0)
            return OperationResult<List<AssignmentDto>>.Success(new List<AssignmentDto>());

        var allAssignments = await _unitOfWork.Assignments
            .FindAsync(a => cstIds.Contains(a.ClassSubjectTeacherId) && !a.IsDeleted);

        var dtos = _mapper.Map<List<AssignmentDto>>(allAssignments);
        return OperationResult<List<AssignmentDto>>.Success(dtos);
    }

        public async Task<OperationResult<IEnumerable<AssignmentSummaryDto>>> GetAssignmentsByClassSubjectTeacherAsync(int classSubjectTeacherId, EvaluationCategory? category = null)
        {
            var cst = await _unitOfWork.ClassSubjectTeachers.GetByIdAsync(classSubjectTeacherId);
            if (cst == null || cst.IsDeleted)
                return OperationResult<IEnumerable<AssignmentSummaryDto>>.Failure("بيان الفصل-المادة-المعلم غير موجود", 404);

            var assignments = await _unitOfWork.Assignments.GetByClassSubjectTeacherIdAsync(classSubjectTeacherId);
            var filtered = assignments.Where(a => !a.IsDeleted);

            if (category.HasValue)
                filtered = filtered.Where(a => a.Category == category.Value);

            var ordered = filtered.OrderByDescending(a => a.DueDate).ToList();
            var dtos = _mapper.Map<IEnumerable<AssignmentSummaryDto>>(ordered);
            return OperationResult<IEnumerable<AssignmentSummaryDto>>.Success(dtos);
        }

        public async Task<OperationResult<AssignmentDto>> GenerateAssignmentWithAIAsync(GenerateAssignmentRequest request)
        {
            var cst = await _unitOfWork.ClassSubjectTeachers.GetByIdAsync(request.ClassSubjectTeacherId);
            if (cst == null || cst.IsDeleted)
                return OperationResult<AssignmentDto>.Failure("بيان الفصل-المادة-المعلم غير موجود", 404);

            if (string.IsNullOrWhiteSpace(request.Topic))
                return OperationResult<AssignmentDto>.Failure("الموضوع مطلوب");

            if (request.QuestionCount < 1 || request.QuestionCount > 50)
                return OperationResult<AssignmentDto>.Failure("عدد الأسئلة يجب أن يكون بين 1 و 50");

            // AI generation would go here — stub that creates an empty assignment with IsAIGenerated = true
            var assignment = new Assignment
            {
                ClassSubjectTeacherId = request.ClassSubjectTeacherId,
                Title = request.Topic,
                Description = $"واجب منشأ بالذكاء الاصطناعي حول {request.Topic}",
                MaxScore = 100,
                IsAutoGraded = true,
                IsAIGenerated = true,
                Category = request.Category,
                DueDate = DateTime.UtcNow.AddDays(7)
            };

            await _unitOfWork.Assignments.AddAsync(assignment);
            await _unitOfWork.SaveChangesAsync();

            var dto = _mapper.Map<AssignmentDto>(assignment);
            return OperationResult<AssignmentDto>.Success(dto, "تم إنشاء الواجب بنجاح");
        }
    }
}