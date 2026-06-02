using AutoMapper;
using Common.Results;
using Project.BLL.DTOs.ResultVisibility;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.Services;

public class ResultVisibilityService : IResultVisibilityService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ResultVisibilityService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<OperationResult<ResultVisibilityDto>> SetVisibilityAsync(SetVisibilityRequest request)
    {
        var academicYear = await _unitOfWork.AcademicYears.GetByIdAsync(request.AcademicYearId);
        if (academicYear == null || academicYear.IsDeleted)
            return OperationResult<ResultVisibilityDto>.Failure("Academic year not found");

        var admin = await _unitOfWork.Users.GetByIdAsync(request.ControlledById);
        if (admin == null || admin.IsDeleted || !admin.IsActive)
            return OperationResult<ResultVisibilityDto>.Failure("Admin user not found or inactive");

        if (admin.Role != UserRole.Admin)
            return OperationResult<ResultVisibilityDto>.Failure("Only Admins can control result visibility");

        if (request.VisibleFrom.HasValue && request.VisibleUntil.HasValue &&
            request.VisibleFrom >= request.VisibleUntil)
            return OperationResult<ResultVisibilityDto>.Failure("VisibleFrom must be before VisibleUntil");

        var setting = new ResultVisibilitySetting
        {
            AcademicYearId = request.AcademicYearId,
            Term = request.Term,
            IsVisible = request.IsVisible,
            VisibleFrom = request.VisibleFrom,
            VisibleUntil = request.VisibleUntil,
            ControlledById = request.ControlledById
        };

        await _unitOfWork.ResultVisibilitySettings.UpsertAsync(setting);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<ResultVisibilityDto>(setting);
        return OperationResult<ResultVisibilityDto>.Success(dto, "Visibility setting saved successfully");
    }

    public async Task<OperationResult<IEnumerable<ResultVisibilityDto>>> GetSettingsByAcademicYearAsync(int academicYearId)
    {
        var settings = await _unitOfWork.ResultVisibilitySettings.GetByAcademicYearIdAsync(academicYearId);
        var dtos = _mapper.Map<IEnumerable<ResultVisibilityDto>>(settings);
        return OperationResult<IEnumerable<ResultVisibilityDto>>.Success(dtos);
    }

    public async Task<OperationResult<bool>> IsResultsVisibleAsync(int academicYearId, AcademicTerm term)
    {
        var isVisible = await _unitOfWork.ResultVisibilitySettings.IsVisibleAsync(academicYearId, term);
        return OperationResult<bool>.Success(isVisible);
    }

    public async Task<OperationResult<IEnumerable<ResultVisibilityDto>>> GetAllSettingsAsync()
    {
        var settings = await _unitOfWork.ResultVisibilitySettings.FindAsync(s => true);
        var dtos = _mapper.Map<IEnumerable<ResultVisibilityDto>>(settings);
        return OperationResult<IEnumerable<ResultVisibilityDto>>.Success(dtos);
    }

    public async Task<OperationResult<ResultVisibilityDto>> UpdateVisibilitySettingAsync(int id, UpdateVisibilityRequest request)
    {
        var setting = await _unitOfWork.ResultVisibilitySettings.GetByIdAsync(id);
        if (setting == null)
            return OperationResult<ResultVisibilityDto>.Failure("Visibility setting not found");

        if (request.VisibleFrom.HasValue && request.VisibleUntil.HasValue &&
            request.VisibleFrom >= request.VisibleUntil)
            return OperationResult<ResultVisibilityDto>.Failure("VisibleFrom must be before VisibleUntil");

        setting.IsVisible = request.IsVisible;
        setting.VisibleFrom = request.VisibleFrom;
        setting.VisibleUntil = request.VisibleUntil;
        setting.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.ResultVisibilitySettings.Update(setting);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<ResultVisibilityDto>(setting);
        return OperationResult<ResultVisibilityDto>.Success(dto, "Visibility setting updated successfully");
    }

    public async Task<OperationResult> DeleteVisibilitySettingAsync(int id)
    {
        var setting = await _unitOfWork.ResultVisibilitySettings.GetByIdAsync(id);
        if (setting == null)
            return OperationResult.Failure("Visibility setting not found");

        _unitOfWork.ResultVisibilitySettings.SoftDelete(setting);
        await _unitOfWork.SaveChangesAsync();
        return OperationResult.Success("Visibility setting deleted successfully");
    }
}