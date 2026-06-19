using AutoMapper;
using Project.BLL.DTOs.Meetings;
using Project.Domain.Entities;

namespace Project.BLL.Mapping;

public class ParentMeetingMappingProfile : Profile
{
    public ParentMeetingMappingProfile()
    {
        CreateMap<CreateMeetingRequest, ParentMeetingRequest>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.Status, o => o.Ignore())
            .ForMember(d => d.HandledById, o => o.Ignore())
            .ForMember(d => d.ScheduledDate, o => o.Ignore())
            .ForMember(d => d.Parent, o => o.Ignore())
            .ForMember(d => d.Student, o => o.Ignore())
            .ForMember(d => d.HandledBy, o => o.Ignore());

        CreateMap<ParentMeetingRequest, ParentMeetingRequestDto>()
            .ForMember(d => d.ParentName, o => o.Ignore())
            .ForMember(d => d.StudentName, o => o.Ignore())
            .ForMember(d => d.HandledByName, o => o.Ignore());
    }
}
