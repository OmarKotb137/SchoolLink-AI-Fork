using Common.Results;
using Project.BLL.DTOs.DailyAbsences;

namespace Project.BLL.Interfaces;

public interface IDailyAbsenceService
{
    Task<OperationResult<DailyAbsenceDto>> RecordAbsenceAsync(RecordAbsenceRequest request);
    Task<OperationResult<DailyAbsenceDto>> UpdateAbsenceAsync(UpdateAbsenceRequest request);
    Task<OperationResult<IEnumerable<DailyAbsenceDto>>> GetAbsencesByEnrollmentAsync(GetAbsenceFilter filter);
    Task<OperationResult<AbsenceSummaryDto>> GetAbsenceSummaryAsync(int enrollmentId, int? classSubjectTeacherId = null);
}
