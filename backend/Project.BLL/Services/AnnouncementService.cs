using AutoMapper;
using Common.Results;
using Project.BLL.DTOs.Announcements;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;

namespace Project.BLL.Services;

public class AnnouncementService : IAnnouncementService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AnnouncementService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<OperationResult<AnnouncementDto>> CreateAnnouncementAsync(CreateAnnouncementRequest request)
    {
        var author = await _unitOfWork.Users.GetByIdAsync(request.AuthorId);
        if (author == null || author.IsDeleted)
            return OperationResult<AnnouncementDto>.Failure("Author not found");

        if (author.Role != Project.Domain.Enums.UserRole.Admin &&
            author.Role != Project.Domain.Enums.UserRole.Teacher)
            return OperationResult<AnnouncementDto>.Failure("Only Admins and Teachers can create announcements");

        if (request.ExpiresAt.HasValue && request.ExpiresAt <= DateTime.UtcNow)
            return OperationResult<AnnouncementDto>.Failure("Expiry date must be in the future");

        var announcement = _mapper.Map<Announcement>(request);
        await _unitOfWork.Announcements.AddAsync(announcement);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<AnnouncementDto>(announcement);
        dto.AuthorName = author.FullName;

        return OperationResult<AnnouncementDto>.Success(dto, "Announcement created successfully");
    }

    public async Task<OperationResult<AnnouncementDto>> GetAnnouncementByIdAsync(int id)
    {
        var announcement = await _unitOfWork.Announcements.GetByIdAsync(id);
        if (announcement == null || announcement.IsDeleted)
            return OperationResult<AnnouncementDto>.Failure($"Announcement with id {id} not found");

        var dto = _mapper.Map<AnnouncementDto>(announcement);
        return OperationResult<AnnouncementDto>.Success(dto, "Announcement retrieved successfully");
    }

    public async Task<OperationResult<AnnouncementDto>> UpdateAnnouncementAsync(int id, CreateAnnouncementRequest request)
    {
        var announcement = await _unitOfWork.Announcements.GetByIdAsync(id);
        if (announcement == null || announcement.IsDeleted)
            return OperationResult<AnnouncementDto>.Failure($"Announcement with id {id} not found");

        var caller = await _unitOfWork.Users.GetByIdAsync(request.AuthorId);
        if (caller == null || caller.IsDeleted)
            return OperationResult<AnnouncementDto>.Failure("Caller not found");

        if (caller.Role != Project.Domain.Enums.UserRole.Admin &&
            caller.Role != Project.Domain.Enums.UserRole.Teacher)
            return OperationResult<AnnouncementDto>.Failure("Only Admins and Teachers can update announcements");

        if (request.ExpiresAt.HasValue && request.ExpiresAt <= DateTime.UtcNow)
            return OperationResult<AnnouncementDto>.Failure("Expiry date must be in the future");

        announcement.Title = request.Title;
        announcement.Body = request.Body;
        announcement.TargetRole = request.TargetRole;
        announcement.TargetClassId = request.TargetClassId;
        announcement.ExpiresAt = request.ExpiresAt;
        announcement.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Announcements.Update(announcement);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<AnnouncementDto>(announcement);
        dto.AuthorName = caller.FullName;

        return OperationResult<AnnouncementDto>.Success(dto, "Announcement updated successfully");
    }

    public async Task<OperationResult> DeleteAnnouncementAsync(int id, int callerUserId)
    {
        var announcement = await _unitOfWork.Announcements.GetByIdAsync(id);
        if (announcement == null || announcement.IsDeleted)
            return OperationResult.Failure($"Announcement with id {id} not found");

        var caller = await _unitOfWork.Users.GetByIdAsync(callerUserId);
        if (caller == null || caller.IsDeleted)
            return OperationResult.Failure("Caller not found");

        if (caller.Role != Project.Domain.Enums.UserRole.Admin && announcement.AuthorId != callerUserId)
            return OperationResult.Failure("Only the author or an Admin can delete this announcement");

        _unitOfWork.Announcements.SoftDelete(announcement);
        await _unitOfWork.SaveChangesAsync();
        return OperationResult.Success("Announcement deleted successfully");
    }

    public async Task<OperationResult<IEnumerable<AnnouncementDto>>> GetExpiredAnnouncementsAsync(int callerUserId)
    {
        var caller = await _unitOfWork.Users.GetByIdAsync(callerUserId);
        if (caller == null)
            return OperationResult<IEnumerable<AnnouncementDto>>.Failure("Caller not found");

        var announcements = await _unitOfWork.Announcements.GetExpiredAsync();
        var dtos = _mapper.Map<IEnumerable<AnnouncementDto>>(announcements.OrderByDescending(a => a.CreatedAt));
        return OperationResult<IEnumerable<AnnouncementDto>>.Success(dtos);
    }

    public async Task<OperationResult<IEnumerable<AnnouncementDto>>> GetActiveAnnouncementsAsync(GetAnnouncementsFilter filter)
    {
        var caller = await _unitOfWork.Users.GetByIdAsync(filter.CallerUserId);
        if (caller == null)
            return OperationResult<IEnumerable<AnnouncementDto>>.Failure("Caller not found");

        var announcements = await _unitOfWork.Announcements.GetActiveAsync();

        var filtered = announcements
            .Where(a => a.TargetRole == null || a.TargetRole == filter.CallerRole);

        if (filter.ClassId.HasValue)
        {
            filtered = filtered.Where(a =>
                a.TargetClassId == null || a.TargetClassId == filter.ClassId.Value);
        }

        var dtos = _mapper.Map<IEnumerable<AnnouncementDto>>(filtered
            .OrderByDescending(a => a.CreatedAt));

        return OperationResult<IEnumerable<AnnouncementDto>>.Success(dtos);
    }
}