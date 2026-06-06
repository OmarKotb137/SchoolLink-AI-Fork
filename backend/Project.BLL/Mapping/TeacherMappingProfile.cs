using AutoMapper;
using Project.BLL.DTOs.Teachers;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.Mapping;

public class TeacherMappingProfile : Profile
{
    public TeacherMappingProfile()
    {
        CreateMap<User, TeacherDto>();

        CreateMap<CreateTeacherRequest, User>()
            .ForMember(d => d.PasswordHash, o => o.Ignore())
            .ForMember(d => d.Role, o => o.MapFrom(_ => UserRole.Teacher))
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore());
    }
}
