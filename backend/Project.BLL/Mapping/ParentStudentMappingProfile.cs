using AutoMapper;
using Project.BLL.DTOs.ParentStudents;
using Project.Domain.Entities;

namespace Project.BLL.Mapping;

public class ParentStudentMappingProfile : Profile
{
    public ParentStudentMappingProfile()
    {
        CreateMap<ParentStudent, ParentStudentDto>()
            .ForMember(d => d.ParentName, o => o.MapFrom(s => s.Parent.FullName))
            .ForMember(d => d.ParentEmail, o => o.MapFrom(s => s.Parent.Email))
            .ForMember(d => d.StudentName, o => o.MapFrom(s => s.Student.FullName));

        CreateMap<LinkParentStudentRequest, ParentStudent>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.Parent, o => o.Ignore())
            .ForMember(d => d.Student, o => o.Ignore());
    }
}
