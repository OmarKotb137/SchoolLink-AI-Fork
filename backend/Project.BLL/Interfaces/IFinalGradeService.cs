using Common.Results;
using Project.BLL.DTOs.FinalGrades;

namespace Project.BLL.Interfaces;

public interface IFinalGradeService
{
    Task<OperationResult<FinalGradeDto>> CalculateFinalGradeAsync(int enrollmentId);
    Task<OperationResult> PublishGradesAsync(PublishGradesRequest request);
    Task<OperationResult<FinalGradeDto>> GetFinalGradeByEnrollmentAsync(int enrollmentId);
    Task<OperationResult<IEnumerable<FinalGradeDto>>> GetFinalGradesByClassAsync(int classId);
    Task<OperationResult<IEnumerable<FinalGradeDto>>> GetTopStudentsAsync(int classId, int count);
    Task<OperationResult<IEnumerable<FinalGradeDto>>> GetStudentsNeedingSupportAsync(int classId, decimal threshold);
    Task<OperationResult<int>> CalculateFinalGradesForClassAsync(int classId);
    Task<OperationResult<IEnumerable<FinalGradeDto>>> GetFinalGradesByAcademicYearAsync(int academicYearId);
}
