using AutoMapper;
using Common.Results;
using Project.BLL.DTOs.Common;
using Project.BLL.DTOs.Notifications;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using SchoolLink.Domain.Entities;

namespace Project.BLL.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public NotificationService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<OperationResult> SendNotificationAsync(SendNotificationRequest request)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);
        if (user == null || user.IsDeleted || !user.IsActive)
            return OperationResult.Failure("Target user not found or inactive");

        var notification = _mapper.Map<Notification>(request);
        await _unitOfWork.Notifications.AddAsync(notification);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("Notification sent successfully");
    }

    public async Task<OperationResult<IEnumerable<NotificationDto>>> GetNotificationsByUserAsync(int userId, bool onlyUnread)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return OperationResult<IEnumerable<NotificationDto>>.Failure("User not found");

        IReadOnlyList<Notification> notifications;
        if (onlyUnread)
            notifications = await _unitOfWork.Notifications.GetUnreadByUserIdAsync(userId);
        else
            notifications = await _unitOfWork.Notifications.FindAsync(n => n.UserId == userId);

        var dtos = _mapper.Map<IEnumerable<NotificationDto>>(notifications
            .OrderByDescending(n => n.CreatedAt));

        return OperationResult<IEnumerable<NotificationDto>>.Success(dtos);
    }

    public async Task<OperationResult<NotificationDto>> GetNotificationByIdAsync(int notificationId, int userId)
    {
        var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId);
        if (notification == null || notification.UserId != userId)
            return OperationResult<NotificationDto>.Failure("Notification not found or does not belong to this user");

        var dto = _mapper.Map<NotificationDto>(notification);
        return OperationResult<NotificationDto>.Success(dto);
    }

    public async Task<OperationResult<int>> GetUnreadCountAsync(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return OperationResult<int>.Failure("User not found");

        var count = await _unitOfWork.Notifications.GetUnreadCountAsync(userId);
        return OperationResult<int>.Success(count);
    }

    public async Task<OperationResult> MarkNotificationAsReadAsync(int notificationId, int userId)
    {
        var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId);
        if (notification == null || notification.UserId != userId)
            return OperationResult.Failure("Notification not found or does not belong to this user");

        await _unitOfWork.Notifications.MarkAsReadAsync(notificationId);
        return OperationResult.Success("Notification marked as read");
    }

    public async Task<OperationResult> MarkAllNotificationsAsReadAsync(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return OperationResult.Failure("User not found");

        await _unitOfWork.Notifications.MarkAllAsReadAsync(userId);
        return OperationResult.Success("All notifications marked as read");
    }

    public async Task<OperationResult<PagedResult<NotificationDto>>> GetNotificationsByUserPagedAsync(int userId, PaginationFilter filter)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return OperationResult<PagedResult<NotificationDto>>.Failure("User not found");

        var notifications = await _unitOfWork.Notifications.GetByUserIdPagedAsync(userId, filter.Page, filter.PageSize);
        var totalCount = await _unitOfWork.Notifications.CountAsync(n => n.UserId == userId);
        var dtos = _mapper.Map<IEnumerable<NotificationDto>>(notifications);

        var paged = new PagedResult<NotificationDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };

        return OperationResult<PagedResult<NotificationDto>>.Success(paged);
    }

    public async Task<OperationResult> SendBulkNotificationAsync(SendBulkNotificationRequest request)
    {
        var notifications = new List<Notification>();
        foreach (var userId in request.UserIds)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null || user.IsDeleted || !user.IsActive)
                continue;

            notifications.Add(new Notification
            {
                UserId = userId,
                Title = request.Title,
                Body = request.Body,
                Type = request.Type,
                DataJson = request.DataJson
            });
        }

        if (notifications.Count == 0)
            return OperationResult.Failure("No valid recipients found");

        await _unitOfWork.Notifications.BulkAddAsync(notifications);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success($"Notifications sent to {notifications.Count} users");
    }

    public async Task<OperationResult> DeleteBulkNotificationsAsync(List<int> notificationIds, int userId)
    {
        foreach (var id in notificationIds)
        {
            var notification = await _unitOfWork.Notifications.GetByIdAsync(id);
            if (notification == null || notification.UserId != userId)
                continue;

            _unitOfWork.Notifications.SoftDelete(notification);
        }

        await _unitOfWork.SaveChangesAsync();
        return OperationResult.Success($"{notificationIds.Count} notifications deleted");
    }

    public async Task<OperationResult> DeleteNotificationAsync(int notificationId, int userId)
    {
        var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId);
        if (notification == null || notification.UserId != userId)
            return OperationResult.Failure("Notification not found or does not belong to this user");

        _unitOfWork.Notifications.SoftDelete(notification);
        await _unitOfWork.SaveChangesAsync();
        return OperationResult.Success("Notification deleted successfully");
    }
}