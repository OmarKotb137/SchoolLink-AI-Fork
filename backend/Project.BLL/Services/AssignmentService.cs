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
                return OperationResult<GetAssignmentDto>.Failure("Assignment not found", 404);

            var dto = _mapper.Map<GetAssignmentDto>(assignment);
            return OperationResult<GetAssignmentDto>.Success(dto);
        }

        public async Task<OperationResult<AssignmentDto>> CreateAsync(CreateAssignmentDto dto)
        {
            var classSubjectTeacher = await _unitOfWork.ClassSubjectTeachers
                .GetByIdAsync(dto.ClassSubjectTeacherId);

            if (classSubjectTeacher == null || classSubjectTeacher.IsDeleted)
                return OperationResult<AssignmentDto>.Failure("ClassSubjectTeacher not found", 404);

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
                return OperationResult<AssignmentDto>.Failure("Assignment not found", 404);

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
                return OperationResult.Failure("Assignment not found");

            _unitOfWork.Assignments.SoftDelete(assignment);
            await _unitOfWork.SaveChangesAsync();

            return OperationResult.Success("Assignment deleted successfully");
        }

        public async Task<OperationResult<AssignmentDto>> AddQuestionAsync(CreateAssignmentQuestionDto dto)
        {
            var assignment = await _unitOfWork.Assignments.GetWithQuestionsAsync(dto.AssignmentId);

            if (assignment == null || assignment.IsDeleted)
                return OperationResult<AssignmentDto>.Failure("Assignment not found", 404);

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
                return OperationResult.Failure("Assignment not found", 404);

            if (assignment.IsPublished)
                return OperationResult.Failure("Assignment is already published");

            assignment.IsPublished = true;
            assignment.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Assignments.Update(assignment);
            await _unitOfWork.SaveChangesAsync();

            return OperationResult.Success("Assignment published successfully");
        }

        public async Task<OperationResult> UnPublishAsync(int id)
        {
            var assignment = await _unitOfWork.Assignments.GetByIdAsync(id);
            if (assignment == null || assignment.IsDeleted)
                return OperationResult.Failure("Assignment not found", 404);

            if (!assignment.IsPublished)
                return OperationResult.Failure("Assignment is not published");

            assignment.IsPublished = false;
            assignment.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Assignments.Update(assignment);
            await _unitOfWork.SaveChangesAsync();

            return OperationResult.Success("Assignment unpublished successfully");
        }

        public async Task<OperationResult> DeleteQuestionAsync(int questionId)
        {
            var question = await _unitOfWork.AssignmentQuestions.GetByIdAsync(questionId);

            if (question == null || question.IsDeleted)
                return OperationResult.Failure("Question not found");

            _unitOfWork.AssignmentQuestions.SoftDelete(question);
            await _unitOfWork.SaveChangesAsync();

            return OperationResult.Success("Question deleted successfully");
        }

        public async Task<OperationResult<List<AssignmentDto>>> GetByTeacherAsync(int teacherId, int academicYearId)
        {
            var teacher = await _unitOfWork.Users.GetByIdAsync(teacherId);
            if (teacher == null || teacher.IsDeleted || teacher.Role != UserRole.Teacher)
                return OperationResult<List<AssignmentDto>>.Failure("Teacher not found", 404);

            var assignments = await _unitOfWork.ClassSubjectTeachers
                .GetByTeacherAndYearAsync(teacherId, academicYearId);

            var result = new List<AssignmentDto>();
            foreach (var cst in assignments)
            {
                var cstAssignments = await _unitOfWork.Assignments
                    .GetByClassSubjectTeacherIdAsync(cst.Id);
                var dtos = _mapper.Map<List<AssignmentDto>>(cstAssignments);
                result.AddRange(dtos);
            }

            return OperationResult<List<AssignmentDto>>.Success(result);
        }

        public async Task<OperationResult<IEnumerable<AssignmentSummaryDto>>> GetAssignmentsByClassSubjectTeacherAsync(int classSubjectTeacherId, EvaluationCategory? category = null)
        {
            var cst = await _unitOfWork.ClassSubjectTeachers.GetByIdAsync(classSubjectTeacherId);
            if (cst == null || cst.IsDeleted)
                return OperationResult<IEnumerable<AssignmentSummaryDto>>.Failure("ClassSubjectTeacher not found", 404);

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
                return OperationResult<AssignmentDto>.Failure("ClassSubjectTeacher not found", 404);

            if (string.IsNullOrWhiteSpace(request.Topic))
                return OperationResult<AssignmentDto>.Failure("Topic is required");

            if (request.QuestionCount < 1 || request.QuestionCount > 50)
                return OperationResult<AssignmentDto>.Failure("Question count must be between 1 and 50");

            // AI generation would go here — stub that creates an empty assignment with IsAIGenerated = true
            var assignment = new Assignment
            {
                ClassSubjectTeacherId = request.ClassSubjectTeacherId,
                Title = request.Topic,
                Description = $"AI-generated assignment on {request.Topic}",
                MaxScore = 100,
                IsAutoGraded = true,
                IsAIGenerated = true,
                Category = request.Category,
                DueDate = DateTime.UtcNow.AddDays(7)
            };

            await _unitOfWork.Assignments.AddAsync(assignment);
            await _unitOfWork.SaveChangesAsync();

            var dto = _mapper.Map<AssignmentDto>(assignment);
            return OperationResult<AssignmentDto>.Success(dto, "Assignment generated successfully");
        }
    }
}