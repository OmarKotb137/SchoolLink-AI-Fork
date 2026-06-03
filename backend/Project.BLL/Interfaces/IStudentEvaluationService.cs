using Common.Results;
using Project.BLL.DTOs.StudentEvaluations;

namespace Project.BLL.Interfaces;

public interface IStudentEvaluationService
{
    Task<OperationResult<StudentEvaluationDto>> RecordEvaluationAsync(RecordEvaluationRequest request);
    Task<OperationResult<StudentEvaluationDto>> UpdateEvaluationAsync(UpdateEvaluationRequest request);
    Task<OperationResult<StudentEvaluationDto>> GetEvaluationByIdAsync(int id);
    Task<OperationResult<IEnumerable<StudentEvaluationDto>>> GetByEnrollmentAndPeriodAsync(int enrollmentId, int periodId);
    Task<OperationResult<IEnumerable<ClassEvaluationDto>>> GetByClassAndPeriodAsync(int classId, int periodId);
    Task<OperationResult> DeleteEvaluationAsync(int id);
    Task<OperationResult> AutoFillAttendanceScoresAsync(int classId, int periodId, int enteredById);
    Task<OperationResult> BulkSaveEvaluationsAsync(BulkSaveEvaluationsRequest request);
}
