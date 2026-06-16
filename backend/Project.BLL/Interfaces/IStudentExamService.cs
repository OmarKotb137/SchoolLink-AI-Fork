using Common.Results;
using Project.BLL.DTOs.StudentExams;

namespace Project.BLL.Interfaces;

public interface IStudentExamService
{
    Task<OperationResult<List<StudentExamListItemDto>>> GetMyExamsAsync(int userId);
    Task<OperationResult<StudentExamDetailsDto>> GetMyExamDetailsAsync(int userId, int examId);
    Task<OperationResult<StudentExamAttemptStartedDto>> StartOrResumeAttemptAsync(int userId, int examId);
    Task<OperationResult<StudentExamAttemptStartedDto>> GetActiveAttemptAsync(int userId, int examId);
    Task<OperationResult<StudentExamAttemptResultDto>> SubmitAttemptAsync(int userId, int attemptId, SubmitStudentExamAttemptDto dto);
    Task<OperationResult<StudentExamAttemptResultDto>> GetAttemptResultAsync(int userId, int attemptId);
}
