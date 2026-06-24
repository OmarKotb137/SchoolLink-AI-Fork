using Common.Results;
using Project.BLL.DTOs.Common;
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
    Task<OperationResult<TeacherGrowthDashboardDto>> GetTeacherGrowthDashboardAsync(AcademicTerm? term = null, int? teacherId = null, int? classId = null);
    Task<OperationResult<TeacherGrowthOverviewDto>> GetTeacherGrowthDashboardOverviewAsync(AcademicTerm? term = null, int? teacherId = null, int? classId = null);
    Task<OperationResult<TeacherGrowthTeachersDto>> GetTeacherGrowthDashboardTeachersAsync(AcademicTerm? term = null, int? teacherId = null, int? classId = null);
    Task<OperationResult<TeacherGrowthStudentPageDto>> GetTeacherGrowthStudentsAsync(int teacherId, int? classId = null, int? subjectId = null, AcademicTerm? term = null, int page = 1, int pageSize = 20);
    Task<OperationResult<List<StudentGrowthWeekDetailDto>>> GetStudentGrowthWeeksAsync(int studentId, int? classId = null, int? subjectId = null, int? teacherId = null, AcademicTerm? term = null);
    Task<OperationResult<StudentGrowthRankingDto>> GetStudentGrowthRankingsAsync(AcademicTerm? term = null);
    Task<OperationResult<StudentExamSummaryDto>> GetStudentExamSummaryAsync(int studentId, AcademicTerm? term = null);
    Task<OperationResult<StudentFinalGradeSummaryDto>> GetStudentFinalGradesAsync(int studentId, AcademicTerm? term = null);
    Task<OperationResult<ClassSubjectTeacherBoardDto>> GetClassSubjectTeacherBoardAsync(AcademicTerm? term = null);
}
