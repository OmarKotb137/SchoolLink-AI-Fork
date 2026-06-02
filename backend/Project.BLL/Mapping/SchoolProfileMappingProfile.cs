using AutoMapper;
using Project.BLL.DTOs.SchoolProfiles;
using Project.Domain.Entities;

namespace Project.BLL.Mapping;

public class SchoolProfileMappingProfile : Profile
{
    public SchoolProfileMappingProfile()
    {
        CreateMap<SchoolProfile, SchoolProfileDto>();
        CreateMap<UpdateSchoolProfileRequest, SchoolProfile>();
    }
}
