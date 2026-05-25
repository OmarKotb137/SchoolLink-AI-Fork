using AutoMapper;
using Project.BLL.DTOs;
using Project.Domain.Entities;

namespace Project.BLL.Mapping;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(d => d.Role, o => o.MapFrom(s => s.Role.ToString()));

        CreateMap<CreateUserDto, User>()
            .ForMember(d => d.PasswordHash, o => o.Ignore());
    }
}
