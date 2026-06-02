using Common.Results;
using Project.BLL.DTOs.AssignmentSubmission;

namespace Project.BLL.Interfaces
{
    public interface IAssignmentSubmissionService
    {
        Task<OperationResult<GetAssignmentSubmissionDto>> GetByIdAsync(int id);
        Task<OperationResult<List<AssignmentSubmissionSummaryDto>>> GetByAssignmentIdAsync(int assignmentId);
        Task<OperationResult<GetAssignmentSubmissionDto>> SubmitAsync(CreateAssignmentSubmissionDto dto);
        Task<OperationResult> GradeAsync(int submissionId);
        Task<OperationResult<GetAssignmentSubmissionDto>> GradeSubmissionAsync(GradeSubmissionRequest request);
        Task<OperationResult<List<AssignmentSubmissionSummaryDto>>> GetByStudentAsync(int enrollmentId);
        Task<OperationResult> ReopenAsync(int submissionId);
    }
}