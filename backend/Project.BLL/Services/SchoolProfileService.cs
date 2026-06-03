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
        if (profile == null || profile.IsDeleted)
            return OperationResult<SchoolProfileDto>.Failure("لم يتم العثور على الملف التعريفي للمدرسة");

        var dto = _mapper.Map<SchoolProfileDto>(profile);
        return OperationResult<SchoolProfileDto>.Success(dto, "تم استرجاع الملف التعريفي بنجاح");
    }

    public async Task<OperationResult<SchoolProfileDto>> UploadLogoAsync(int adminUserId, string logoPath)
    {
        var admin = await _unitOfWork.Users.GetByIdAsync(adminUserId);
        if (admin == null || admin.IsDeleted || !admin.IsActive)
            return OperationResult<SchoolProfileDto>.Failure("المستخدم المسؤول غير موجود أو غير نشط");

        var profile = await _unitOfWork.SchoolProfiles.GetActiveAsync();
        if (profile == null || profile.IsDeleted)
            return OperationResult<SchoolProfileDto>.Failure("لم يتم العثور على الملف التعريفي للمدرسة. قم بإنشاء ملف تعريفي أولاً.");

        profile.LogoPath = logoPath;
        profile.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.SchoolProfiles.Update(profile);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<SchoolProfileDto>(profile);
        return OperationResult<SchoolProfileDto>.Success(dto, "تم رفع الشعار بنجاح");
    }

    public async Task<OperationResult> DeleteLogoAsync(int adminUserId)
    {
        var admin = await _unitOfWork.Users.GetByIdAsync(adminUserId);
        if (admin == null || admin.IsDeleted || !admin.IsActive)
            return OperationResult.Failure("المستخدم المسؤول غير موجود أو غير نشط");

        var profile = await _unitOfWork.SchoolProfiles.GetActiveAsync();
        if (profile == null || profile.IsDeleted || string.IsNullOrEmpty(profile.LogoPath))
            return OperationResult.Failure("لا يوجد شعار للحذف");

        profile.LogoPath = null;
        profile.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.SchoolProfiles.Update(profile);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("تم حذف الشعار بنجاح");
    }

    public async Task<OperationResult<SchoolProfileDto>> UpdateProfileAsync(UpdateSchoolProfileRequest request, int adminUserId)
    {
        var admin = await _unitOfWork.Users.GetByIdAsync(adminUserId);
        if (admin == null || admin.IsDeleted || !admin.IsActive)
            return OperationResult<SchoolProfileDto>.Failure("المستخدم المسؤول غير موجود أو غير نشط");

        var profile = await _unitOfWork.SchoolProfiles.GetActiveAsync();
        var isNew = profile == null || profile.IsDeleted;

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
        return OperationResult<SchoolProfileDto>.Success(dto, isNew ? "تم إنشاء الملف التعريفي بنجاح" : "تم تحديث الملف التعريفي بنجاح");
    }
}
