using Common.Results;
using Project.BLL.DTOs.SchoolProfiles;

namespace Project.BLL.Interfaces;

public interface ISchoolProfileService
{
    Task<OperationResult<SchoolProfileDto>> GetActiveProfileAsync();
    Task<OperationResult<SchoolProfileDto>> UpdateProfileAsync(UpdateSchoolProfileRequest request, int adminUserId);
}
