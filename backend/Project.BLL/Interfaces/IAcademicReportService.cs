using Common.Results;
using Project.BLL.DTOs.Reports;
using Project.Domain.Enums;

namespace Project.BLL.Interfaces;

public interface IAcademicReportService
{
    /// <summary>
    /// Returns a complete academic report (weekly scores, final grades, monthly exams,
    /// summary stats) for a given class, term, and subject.
    /// If gradeLevelId is provided instead of classId, aggregates data across all classes
    /// of that grade level.
    /// </summary>
    Task<OperationResult<AcademicReportDto>> GetAcademicReportAsync(
        int classId,
        AcademicTerm term,
        int subjectId,
        int? gradeLevelId = null);
}
