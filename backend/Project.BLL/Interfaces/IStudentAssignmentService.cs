using Common.Results;
using Project.BLL.DTOs.StudentAssignments;

namespace Project.BLL.Interfaces;

public interface IStudentAssignmentService
{
    Task<OperationResult<List<StudentAssignmentListItemDto>>> GetMyAssignmentsAsync(int userId, string? status = null, int? subjectId = null);
    Task<OperationResult<StudentAssignmentDetailsDto>> GetMyAssignmentDetailsAsync(int userId, int assignmentId);
    Task<OperationResult<StudentAssignmentSubmissionResultDto>> SubmitAssignmentAsync(int userId, int assignmentId, SubmitStudentAssignmentDto dto);
    Task<OperationResult<StudentAssignmentSubmissionResultDto>> GetSubmissionResultAsync(int userId, int submissionId);
}
