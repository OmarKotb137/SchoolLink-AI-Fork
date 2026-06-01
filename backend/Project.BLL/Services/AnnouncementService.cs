using AutoMapper;
using Common.Results;
using Project.BLL.DTOs.Announcements;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using SchoolLink.Domain.Entities;

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

        if (author.Role != SchoolLink.Domain.Enums.UserRole.Admin &&
            author.Role != SchoolLink.Domain.Enums.UserRole.Teacher)
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