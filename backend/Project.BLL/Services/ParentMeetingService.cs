using AutoMapper;
using Common.Results;
using Project.BLL.DTOs.Meetings;
using Project.BLL.DTOs.Notifications;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.Services;

public class ParentMeetingService : IParentMeetingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly INotificationService _notificationService;

    public ParentMeetingService(IUnitOfWork unitOfWork, IMapper mapper, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _notificationService = notificationService;
    }

    public async Task<OperationResult<ParentMeetingRequestDto>> CreateRequestAsync(CreateMeetingRequest request)
    {
        var parent = await _unitOfWork.Users.GetByIdAsync(request.ParentId);
        if (parent == null || parent.IsDeleted || !parent.IsActive)
            return OperationResult<ParentMeetingRequestDto>.Failure("ولي الأمر غير موجود");

        var student = await _unitOfWork.Students.GetByIdAsync(request.StudentId);
        if (student == null || student.IsDeleted)
            return OperationResult<ParentMeetingRequestDto>.Failure("الطالب غير موجود");

        var entity = new ParentMeetingRequest
        {
            ParentId = request.ParentId,
            StudentId = request.StudentId,
            Reason = request.Reason,
            PreferredDate = request.PreferredDate,
            Notes = request.Notes,
            Status = MeetingRequestStatus.Pending
        };

        await _unitOfWork.ParentMeetingRequests.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        // Notify admins + الطالب صاحب الشأن
        var recipients = new List<int>();
        var admins = await _unitOfWork.Users.FindAsync(u =>
            u.Role == UserRole.Admin && u.IsActive && !u.IsDeleted);
        recipients.AddRange(admins.Select(a => a.Id));
        if (student.UserId != null)
            recipients.Add(student.UserId.Value);

        recipients = recipients.Distinct().ToList();
        if (recipients.Count != 0)
        {
            await _notificationService.SendBulkNotificationAsync(new SendBulkNotificationRequest
            {
                UserIds = recipients,
                Title = "طلب اجتماع مع ولي أمر",
                Body = $"طلب ولي الأمر {parent.FullName} اجتماعاً بخصوص الطالب {student.FullName}",
                Type = NotificationType.ParentMeetingRequest
            });
        }

        var dto = _mapper.Map<ParentMeetingRequestDto>(entity);
        dto.ParentName = parent.FullName;
        dto.StudentName = student.FullName;

        return OperationResult<ParentMeetingRequestDto>.Success(dto, "تم إنشاء طلب الاجتماع بنجاح");
    }

    public async Task<OperationResult<ParentMeetingRequestDto>> ApproveRequestAsync(int requestId, int adminId, DateTime scheduledDate)
    {
        var request = await _unitOfWork.ParentMeetingRequests.GetByIdAsync(requestId);
        if (request == null || request.IsDeleted)
            return OperationResult<ParentMeetingRequestDto>.Failure("طلب الاجتماع غير موجود");

        if (request.Status == MeetingRequestStatus.Approved)
            return OperationResult<ParentMeetingRequestDto>.Failure("طلب الاجتماع تمت الموافقة عليه مسبقاً");

        var admin = await _unitOfWork.Users.GetByIdAsync(adminId);
        if (admin == null || admin.IsDeleted)
            return OperationResult<ParentMeetingRequestDto>.Failure("المستخدم غير موجود");

        request.Status = MeetingRequestStatus.Approved;
        request.HandledById = adminId; // بنحفظ مين اللي وافق
        request.ScheduledDate = scheduledDate;
        _unitOfWork.ParentMeetingRequests.Update(request);
        await _unitOfWork.SaveChangesAsync();

        // Notify parent
        await _notificationService.SendNotificationAsync(new SendNotificationRequest
        {
            UserId = request.ParentId,
            Title = "تمت الموافقة على طلب الاجتماع",
            Body = $"تمت الموافقة على طلب الاجتماع بتاريخ {scheduledDate:yyyy-MM-dd HH:mm}",
            Type = NotificationType.ParentMeetingRequest
        });

        var dto = await BuildDtoAsync(request);
        return OperationResult<ParentMeetingRequestDto>.Success(dto, "تمت الموافقة على طلب الاجتماع");
    }

    public async Task<OperationResult<ParentMeetingRequestDto>> RejectRequestAsync(int requestId, int adminId, string? reason)
    {
        var request = await _unitOfWork.ParentMeetingRequests.GetByIdAsync(requestId);
        if (request == null || request.IsDeleted)
            return OperationResult<ParentMeetingRequestDto>.Failure("طلب الاجتماع غير موجود");

        if (request.Status == MeetingRequestStatus.Rejected)
            return OperationResult<ParentMeetingRequestDto>.Failure("طلب الاجتماع تم رفضه مسبقاً");

        request.Status = MeetingRequestStatus.Rejected;
        request.HandledById = adminId; // بنحفظ مين اللي رفض
        request.Notes = reason;
        _unitOfWork.ParentMeetingRequests.Update(request);
        await _unitOfWork.SaveChangesAsync();

        await _notificationService.SendNotificationAsync(new SendNotificationRequest
        {
            UserId = request.ParentId,
            Title = "رفض طلب الاجتماع",
            Body = $"تم رفض طلب الاجتماع{(!string.IsNullOrEmpty(reason) ? $": {reason}" : "")}",
            Type = NotificationType.ParentMeetingRequest
        });

        var dto = await BuildDtoAsync(request);
        return OperationResult<ParentMeetingRequestDto>.Success(dto, "تم رفض طلب الاجتماع");
    }

    public async Task<OperationResult<ParentMeetingRequestDto>> CompleteRequestAsync(int requestId, int userId)
    {
        var request = await _unitOfWork.ParentMeetingRequests.GetByIdAsync(requestId);
        if (request == null || request.IsDeleted)
            return OperationResult<ParentMeetingRequestDto>.Failure("طلب الاجتماع غير موجود");

        if (request.Status == MeetingRequestStatus.Completed)
            return OperationResult<ParentMeetingRequestDto>.Failure("طلب الاجتماع تم إنهاؤه مسبقاً");

        request.Status = MeetingRequestStatus.Completed;
        _unitOfWork.ParentMeetingRequests.Update(request);
        await _unitOfWork.SaveChangesAsync();

        var dto = await BuildDtoAsync(request);
        return OperationResult<ParentMeetingRequestDto>.Success(dto, "تم إنهاء طلب الاجتماع");
    }

    public async Task<OperationResult<IEnumerable<ParentMeetingRequestDto>>> GetRequestsByParentAsync(int parentId)
    {
        var requests = await _unitOfWork.ParentMeetingRequests
            .FindAsync(r => r.ParentId == parentId && !r.IsDeleted);

        var dtos = new List<ParentMeetingRequestDto>();
        foreach (var request in requests.OrderByDescending(r => r.CreatedAt))
            dtos.Add(await BuildDtoAsync(request));

        return OperationResult<IEnumerable<ParentMeetingRequestDto>>.Success(dtos);
    }

    public async Task<OperationResult<IEnumerable<ParentMeetingRequestDto>>> GetRequestsByTeacherAsync(int teacherId)
    {
        var requests = await _unitOfWork.ParentMeetingRequests
            .FindAsync(r => r.HandledById == teacherId && !r.IsDeleted);

        var dtos = new List<ParentMeetingRequestDto>();
        foreach (var request in requests.OrderByDescending(r => r.CreatedAt))
            dtos.Add(await BuildDtoAsync(request));

        return OperationResult<IEnumerable<ParentMeetingRequestDto>>.Success(dtos);
    }

    public async Task<OperationResult<IEnumerable<ParentMeetingRequestDto>>> GetAllRequestsAsync()
    {
        var requests = await _unitOfWork.ParentMeetingRequests
            .FindAsync(r => !r.IsDeleted);

        var dtos = new List<ParentMeetingRequestDto>();
        foreach (var request in requests.OrderByDescending(r => r.CreatedAt))
            dtos.Add(await BuildDtoAsync(request));

        return OperationResult<IEnumerable<ParentMeetingRequestDto>>.Success(dtos);
    }

    public async Task<OperationResult<ParentMeetingRequestDto>> GetRequestByIdAsync(int requestId)
    {
        var request = await _unitOfWork.ParentMeetingRequests.GetByIdAsync(requestId);
        if (request == null || request.IsDeleted)
            return OperationResult<ParentMeetingRequestDto>.Failure("طلب الاجتماع غير موجود");

        var dto = await BuildDtoAsync(request);
        return OperationResult<ParentMeetingRequestDto>.Success(dto);
    }

    private async Task<ParentMeetingRequestDto> BuildDtoAsync(ParentMeetingRequest request)
    {
        var dto = _mapper.Map<ParentMeetingRequestDto>(request);

        var parent = await _unitOfWork.Users.GetByIdAsync(request.ParentId);
        dto.ParentName = parent?.FullName ?? "";

        var student = await _unitOfWork.Students.GetByIdAsync(request.StudentId);
        dto.StudentName = student?.FullName ?? "";

        if (request.HandledById.HasValue)
        {
            var handler = await _unitOfWork.Users.GetByIdAsync(request.HandledById.Value);
            dto.HandledByName = handler?.FullName ?? "";
        }

        return dto;
    }
}
