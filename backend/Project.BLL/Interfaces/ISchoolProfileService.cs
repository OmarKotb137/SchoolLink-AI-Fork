using Common.Results;
using Project.BLL.DTOs.SchoolProfiles;

namespace Project.BLL.Interfaces;

public interface ISchoolProfileService
{
    Task<OperationResult<SchoolProfileDto>> GetActiveProfileAsync();
    Task<OperationResult<SchoolProfileDto>> UpdateProfileAsync(UpdateSchoolProfileRequest request, int adminUserId);
    Task<OperationResult<SchoolProfileDto>> UploadLogoAsync(int adminUserId, string logoUrl);
    Task<OperationResult> DeleteLogoAsync(int adminUserId);
}
