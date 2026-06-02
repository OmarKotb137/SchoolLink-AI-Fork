using Common.Results;
using Project.BLL.DTOs.Assignment;
using Project.BLL.DTOs.AssignmentQuestion;

namespace Project.BLL.Interfaces
{
    public interface IAssignmentService
    {
        Task<OperationResult<List<AssignmentDto>>> GetAllByClassSubjectTeacherAsync(int classSubjectTeacherId);
        Task<OperationResult<GetAssignmentDto>> GetByIdAsync(int id);
        Task<OperationResult<AssignmentDto>> CreateAsync(CreateAssignmentDto dto);
        Task<OperationResult<AssignmentDto>> UpdateAsync(UpdateAssignmentDto dto);
        Task<OperationResult> DeleteAsync(int id);
        Task<OperationResult<AssignmentDto>> AddQuestionAsync(CreateAssignmentQuestionDto dto);
        Task<OperationResult> DeleteQuestionAsync(int questionId);
    }
}