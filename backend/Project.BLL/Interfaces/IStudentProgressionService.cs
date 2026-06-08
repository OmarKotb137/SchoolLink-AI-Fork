using Common.Results;
using Project.BLL.DTOs.Enrollments;

namespace Project.BLL.Interfaces;

public interface IStudentProgressionService
{
    Task<OperationResult<IEnumerable<StudentProgressionCandidateDto>>> GetCandidatesAsync(
        int gradeLevelId,
        int academicYearId,
        CancellationToken ct = default);

    Task<OperationResult<StudentProgressionResultDto>> ExecuteAsync(
        StudentProgressionRequest request,
        CancellationToken ct = default);
}
