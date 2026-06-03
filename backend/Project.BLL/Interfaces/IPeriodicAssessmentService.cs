using Common.Results;
using Project.BLL.DTOs.PeriodicAssessments;

namespace Project.BLL.Interfaces;

public interface IPeriodicAssessmentService
{
    Task<OperationResult<PeriodicAssessmentDto>> RecordPeriodicAssessmentAsync(RecordPeriodicAssessmentRequest request);
    Task<OperationResult<PeriodicAssessmentDto>> UpdatePeriodicAssessmentAsync(UpdatePeriodicAssessmentRequest request);
    Task<OperationResult<IEnumerable<PeriodicAssessmentDto>>> GetByEnrollmentAsync(int enrollmentId);
}
