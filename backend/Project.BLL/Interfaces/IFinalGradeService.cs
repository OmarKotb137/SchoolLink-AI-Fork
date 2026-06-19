using Common.Results;
using Project.BLL.DTOs.FinalGrades;
using Project.Domain.Enums;

namespace Project.BLL.Interfaces;

public interface IFinalGradeService
{
    Task<OperationResult<FinalGradeDto>> CalculateFinalGradeAsync(int enrollmentId, AcademicTerm? term = null, int? subjectId = null);
    Task<OperationResult> PublishGradesAsync(PublishGradesRequest request);
    Task<OperationResult<FinalGradeDto>> GetFinalGradeByEnrollmentAsync(int enrollmentId, AcademicTerm? term = null);
    Task<OperationResult<IEnumerable<FinalGradeDto>>> GetFinalGradesByClassAsync(int classId, AcademicTerm? term = null, int? subjectId = null);
    Task<OperationResult<IEnumerable<FinalGradeDto>>> GetTopStudentsAsync(int classId, int count, AcademicTerm? term = null);
    Task<OperationResult<IEnumerable<FinalGradeDto>>> GetStudentsNeedingSupportAsync(int classId, decimal threshold, AcademicTerm? term = null);
    Task<OperationResult<int>> CalculateFinalGradesForClassAsync(int classId, AcademicTerm? term = null);
    Task<OperationResult<IEnumerable<FinalGradeDto>>> GetFinalGradesByAcademicYearAsync(int academicYearId, AcademicTerm? term = null);
    Task<OperationResult<IEnumerable<FinalGradeDto>>> CalculateFullForClassAsync(int classId, CalculateFullFinalGradesRequest request);
    Task<OperationResult<IEnumerable<FinalGradeDto>>> RecalculateForClassAsync(int classId, AcademicTerm? term = null, int? subjectId = null);
}
