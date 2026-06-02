using Common.Results;
using Project.BLL.DTOs.ExamAttempt;

namespace Project.BLL.Interfaces
{
    public interface IExamAttemptService
    {
        Task<OperationResult<GetExamAttemptDto>> GetByIdAsync(int id);
        Task<OperationResult<List<ExamAttemptSummaryDto>>> GetByExamIdAsync(int examId);
        Task<OperationResult<GetExamAttemptDto>> StartAttemptAsync(CreateExamAttemptDto dto);
        Task<OperationResult<GetExamAttemptDto>> SubmitAttemptAsync(SubmitExamAttemptDto dto);
        Task<OperationResult> GradeAttemptAsync(int attemptId);
        Task<OperationResult<List<ExamAttemptSummaryDto>>> GetStudentAttemptsAsync(int enrollmentId, int examId);
        Task<OperationResult> AutoGradeAsync(int attemptId);
    }
}