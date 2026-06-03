using Common.Results;
using Project.BLL.DTOs.StudentEvaluations;

namespace Project.BLL.Interfaces;

public interface IStudentEvaluationService
{
    Task<OperationResult<StudentEvaluationDto>> RecordEvaluationAsync(RecordEvaluationRequest request);
    Task<OperationResult<StudentEvaluationDto>> UpdateEvaluationAsync(UpdateEvaluationRequest request);
    Task<OperationResult<IEnumerable<StudentEvaluationDto>>> GetByEnrollmentAndPeriodAsync(int enrollmentId, int periodId);
    Task<OperationResult<IEnumerable<ClassEvaluationDto>>> GetByClassAndPeriodAsync(int classId, int periodId);
}
