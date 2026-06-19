using Common.Results;
using Project.BLL.DTOs.PeriodicAssessments;
using Project.Domain.Enums;

namespace Project.BLL.Interfaces;

public interface IPeriodicAssessmentService
{
    Task<OperationResult<PeriodicAssessmentDto>> RecordPeriodicAssessmentAsync(RecordPeriodicAssessmentRequest request);
    Task<OperationResult<PeriodicAssessmentDto>> UpdatePeriodicAssessmentAsync(UpdatePeriodicAssessmentRequest request);
    Task<OperationResult> DeletePeriodicAssessmentAsync(int id);
    Task<OperationResult<IEnumerable<PeriodicAssessmentDto>>> GetByEnrollmentAsync(int enrollmentId, AcademicTerm? term = null);
    Task<OperationResult<PeriodicAssessmentDto>> GetPeriodicAssessmentByIdAsync(int id);
    Task<OperationResult<IEnumerable<PeriodicAssessmentDto>>> GetByClassAsync(int classId, AcademicTerm? term = null, int? subjectId = null);
}
