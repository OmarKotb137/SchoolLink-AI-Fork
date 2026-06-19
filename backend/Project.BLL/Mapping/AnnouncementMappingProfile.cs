using AutoMapper;
using Project.BLL.DTOs.Announcements;
using Project.Domain.Entities;
namespace Project.BLL.Mapping;

public class AnnouncementMappingProfile : Profile
{
    public AnnouncementMappingProfile()
    {
        CreateMap<Announcement, AnnouncementDto>()
            .ForMember(d => d.AuthorName, o => o.MapFrom(s => s.Author.FullName))
            .ForMember(d => d.TargetedUserCount, o => o.Ignore());

        CreateMap<CreateAnnouncementRequest, Announcement>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.Author, o => o.Ignore())
            .ForMember(d => d.TargetClass, o => o.Ignore());
    }
}
