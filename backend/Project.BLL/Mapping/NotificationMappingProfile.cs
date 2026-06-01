using AutoMapper;
using Project.BLL.DTOs.Notifications;
using SchoolLink.Domain.Entities;

namespace Project.BLL.Mapping;

public class NotificationMappingProfile : Profile
{
    public NotificationMappingProfile()
    {
        CreateMap<Notification, NotificationDto>();

        CreateMap<SendNotificationRequest, Notification>()
            .ForMember(d => d.IsRead, o => o.Ignore())
            .ForMember(d => d.ReadAt, o => o.Ignore())
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.User, o => o.Ignore());
    }
}
