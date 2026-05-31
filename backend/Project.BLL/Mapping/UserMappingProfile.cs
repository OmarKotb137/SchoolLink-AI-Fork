using AutoMapper;
using Project.BLL.DTOs;
using SchoolLink.Domain.Entities;

namespace Project.BLL.Mapping;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(d => d.FirstName, o => o.MapFrom(s => GetFirstName(s.FullName)))
            .ForMember(d => d.LastName, o => o.MapFrom(s => GetLastName(s.FullName)))
            .ForMember(d => d.Role, o => o.MapFrom(s => s.Role.ToString()));

        CreateMap<CreateUserDto, User>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => $"{s.FirstName} {s.LastName}".Trim()))
            .ForMember(d => d.PasswordHash, o => o.Ignore());
    }

    private static string GetFirstName(string fullName)
    {
        var parts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0] : string.Empty;
    }

    private static string GetLastName(string fullName)
    {
        var parts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? parts[1] : string.Empty;
    }
}
