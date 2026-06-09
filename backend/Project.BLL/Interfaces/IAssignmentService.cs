using Common.Results;
using Project.BLL.DTOs.Assignment;
using Project.BLL.DTOs.AssignmentQuestion;
using Project.Domain.Enums;

namespace Project.BLL.Interfaces
{
    public interface IAssignmentService
    {
        Task<OperationResult<List<AssignmentDto>>> GetAllByClassSubjectTeacherAsync(int classSubjectTeacherId);
        Task<OperationResult<GetAssignmentDto>> GetByIdAsync(int id);
        Task<OperationResult<AssignmentDto>> CreateAsync(CreateAssignmentDto dto);
        Task<OperationResult<AssignmentDto>> UpdateAsync(UpdateAssignmentDto dto);
        Task<OperationResult> DeleteAsync(int id);
        Task<OperationResult> PublishAsync(int id);
        Task<OperationResult> UnPublishAsync(int id);
        Task<OperationResult<AssignmentDto>> AddQuestionAsync(CreateAssignmentQuestionDto dto);
        Task<OperationResult> UpdateQuestionAsync(UpdateAssignmentQuestionDto dto);
        Task<OperationResult> DeleteQuestionAsync(int questionId);
        Task<OperationResult<List<AssignmentDto>>> GetByTeacherAsync(int teacherId, int academicYearId);
        Task<OperationResult<IEnumerable<AssignmentSummaryDto>>> GetAssignmentsByClassSubjectTeacherAsync(int classSubjectTeacherId, EvaluationCategory? category = null);
        Task<OperationResult<AssignmentDto>> GenerateAssignmentWithAIAsync(GenerateAssignmentRequest request);
        Task<OperationResult<List<AssignmentWithSubmissionDto>>> GetByEnrollmentAsync(int enrollmentId);
    }
}