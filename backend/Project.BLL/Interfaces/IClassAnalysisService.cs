using Common.Results;
using Project.BLL.DTOs.ClassAnalysis;
using Project.Domain.Enums;

namespace Project.BLL.Interfaces;

public interface IClassAnalysisService
{
    Task<OperationResult<ClassAnalysisFullDto>> GetFullAnalysisAsync(int classId, AcademicTerm? term = null);
    Task<OperationResult<ClassAnalysisOverviewDto>> GetOverviewAsync(int classId, AcademicTerm? term = null);
    Task<OperationResult<List<SubjectPerformanceDto>>> GetSubjectPerformanceAsync(int classId, AcademicTerm? term = null);
    Task<OperationResult<List<AttendanceTrendDto>>> GetAttendanceTrendsAsync(int classId, AcademicTerm? term = null);
    Task<OperationResult<List<TopStudentDto>>> GetTopStudentsAsync(int classId, int count = 10, AcademicTerm? term = null);
    Task<OperationResult<List<AtRiskStudentDto>>> GetAtRiskStudentsAsync(int classId, AcademicTerm? term = null);
    Task<OperationResult<List<WeaknessDto>>> GetWeaknessAnalysisAsync(int classId, AcademicTerm? term = null);
    Task<OperationResult<List<ClassStudentListDto>>> GetStudentsAsync(int classId, AcademicTerm? term = null);
}
