using AutoMapper;
using Project.BLL.DTOs.AIGenerationLog;
using Project.Domain.Entities;

namespace Project.BLL.Mapping
{
    public class AIGenerationLogMappingProfile : Profile
    {
        public AIGenerationLogMappingProfile()
        {
            // AIGenerationLog → GetAIGenerationLogDto
            CreateMap<AIGenerationLog, GetAIGenerationLogDto>()
                .ForMember(dest => dest.UserName,
                    opt => opt.MapFrom(src => src.User != null ? src.User.FullName : string.Empty));

            // CreateAIGenerationLogDto → AIGenerationLog
            CreateMap<CreateAIGenerationLogDto, AIGenerationLog>()
                .ForMember(d => d.User, opt => opt.Ignore())
                .ForMember(d => d.IsDeleted, opt => opt.Ignore())
                .ForMember(d => d.CreatedAt, opt => opt.Ignore())
                .ForMember(d => d.UpdatedAt, opt => opt.Ignore());
        }
    }
}