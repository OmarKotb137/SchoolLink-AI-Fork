using AutoMapper;
using Project.BLL.DTOs.ResultVisibility;
using Project.Domain.Entities;

namespace Project.BLL.Mapping;

public class ResultVisibilityMappingProfile : Profile
{
    public ResultVisibilityMappingProfile()
    {
        CreateMap<ResultVisibilitySetting, ResultVisibilityDto>();
    }
}
