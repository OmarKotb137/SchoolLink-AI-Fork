using AutoMapper;
using Project.BLL.DTOs.Users;
using Project.Domain.Entities;

namespace Project.BLL.Mapping;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(d => d.Role, o => o.MapFrom(s => s.Role.ToString()));

        CreateMap<CreateUserRequest, User>()
            .ForMember(d => d.PasswordHash, o => o.Ignore())
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore());
    }
}
