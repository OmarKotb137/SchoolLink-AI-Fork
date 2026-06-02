using Common.Results;
using Project.BLL.DTOs.Exam;

namespace Project.BLL.Interfaces
{
    public interface IExamService
    {
        Task<OperationResult<List<ExamSummaryDto>>> GetAllByClassSubjectTeacherAsync(int classSubjectTeacherId);
        Task<OperationResult<GetExamDto>> GetByIdAsync(int id);
        Task<OperationResult<ExamSummaryDto>> CreateAsync(CreateExamDto dto);
        Task<OperationResult<ExamSummaryDto>> UpdateAsync(UpdateExamDto dto);
        Task<OperationResult> DeleteAsync(int id);
        Task<OperationResult> PublishAsync(int id);
        Task<OperationResult> UnPublishAsync(int id);
        Task<OperationResult<List<ExamSummaryDto>>> GetExamsByStudentAsync(int enrollmentId);
        Task<OperationResult<List<ExamSummaryDto>>> GetUpcomingExamsAsync(int classId, int academicYearId);
    }
}