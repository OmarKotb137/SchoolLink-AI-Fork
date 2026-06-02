using Common.Results;
using Project.BLL.DTOs.ResultVisibility;
using Project.Domain.Enums;

namespace Project.BLL.Interfaces;

public interface IResultVisibilityService
{
    Task<OperationResult<ResultVisibilityDto>> SetVisibilityAsync(SetVisibilityRequest request);
    Task<OperationResult<bool>> IsResultsVisibleAsync(int academicYearId, AcademicTerm term);
    Task<OperationResult<IEnumerable<ResultVisibilityDto>>> GetSettingsByAcademicYearAsync(int academicYearId);
}
