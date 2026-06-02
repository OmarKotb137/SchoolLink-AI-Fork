using AutoMapper;
using Common.Results;
using Project.BLL.DTOs.SchoolProfiles;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;

namespace Project.BLL.Services;

public class SchoolProfileService : ISchoolProfileService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public SchoolProfileService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<OperationResult<SchoolProfileDto>> GetActiveProfileAsync()
    {
        var profile = await _unitOfWork.SchoolProfiles.GetActiveAsync();
        if (profile == null)
            return OperationResult<SchoolProfileDto>.Failure("School profile not found");

        var dto = _mapper.Map<SchoolProfileDto>(profile);
        return OperationResult<SchoolProfileDto>.Success(dto, "Profile retrieved successfully");
    }

    public async Task<OperationResult<SchoolProfileDto>> UploadLogoAsync(int adminUserId, string logoPath)
    {
        var admin = await _unitOfWork.Users.GetByIdAsync(adminUserId);
        if (admin == null || admin.IsDeleted || !admin.IsActive)
            return OperationResult<SchoolProfileDto>.Failure("Admin user not found or inactive");

        var profile = await _unitOfWork.SchoolProfiles.GetActiveAsync();
        if (profile == null)
            return OperationResult<SchoolProfileDto>.Failure("School profile not found. Create a profile first.");

        profile.LogoPath = logoPath;
        profile.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.SchoolProfiles.Update(profile);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<SchoolProfileDto>(profile);
        return OperationResult<SchoolProfileDto>.Success(dto, "Logo uploaded successfully");
    }

    public async Task<OperationResult> DeleteLogoAsync(int adminUserId)
    {
        var admin = await _unitOfWork.Users.GetByIdAsync(adminUserId);
        if (admin == null || admin.IsDeleted || !admin.IsActive)
            return OperationResult.Failure("Admin user not found or inactive");

        var profile = await _unitOfWork.SchoolProfiles.GetActiveAsync();
        if (profile == null || string.IsNullOrEmpty(profile.LogoPath))
            return OperationResult.Failure("No logo to delete");

        profile.LogoPath = null;
        profile.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.SchoolProfiles.Update(profile);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("Logo deleted successfully");
    }

    public async Task<OperationResult<SchoolProfileDto>> UpdateProfileAsync(UpdateSchoolProfileRequest request, int adminUserId)
    {
        var admin = await _unitOfWork.Users.GetByIdAsync(adminUserId);
        if (admin == null || admin.IsDeleted || !admin.IsActive)
            return OperationResult<SchoolProfileDto>.Failure("Admin user not found or inactive");

        var profile = await _unitOfWork.SchoolProfiles.GetActiveAsync();
        var isNew = profile == null;

        if (isNew)
        {
            profile = _mapper.Map<SchoolProfile>(request);
            profile.IsActive = true;
            await _unitOfWork.SchoolProfiles.AddAsync(profile);
        }
        else
        {
            _mapper.Map(request, profile);
            profile.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.SchoolProfiles.Update(profile);
        }

        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<SchoolProfileDto>(profile);
        return OperationResult<SchoolProfileDto>.Success(dto, isNew ? "Profile created successfully" : "Profile updated successfully");
    }
}
