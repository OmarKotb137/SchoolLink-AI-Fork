using Common.Results;
using Project.BLL.DTOs.ResultVisibility;
using SchoolLink.Domain.Enums;

namespace Project.BLL.Interfaces;

public interface IResultVisibilityService
{
    Task<OperationResult<ResultVisibilityDto>> SetVisibilityAsync(SetVisibilityRequest request);
    Task<OperationResult<bool>> IsResultsVisibleAsync(int academicYearId, AcademicTerm term);
    Task<OperationResult<IEnumerable<ResultVisibilityDto>>> GetSettingsByAcademicYearAsync(int academicYearId);
    Task<OperationResult<IEnumerable<ResultVisibilityDto>>> GetAllSettingsAsync();
    Task<OperationResult<ResultVisibilityDto>> UpdateVisibilitySettingAsync(int id, UpdateVisibilityRequest request);
    Task<OperationResult> DeleteVisibilitySettingAsync(int id);
}
