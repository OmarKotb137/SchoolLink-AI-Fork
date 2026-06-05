using AutoMapper;
using Project.BLL.DTOs.StudyPlans;
using Project.Domain.Entities;

namespace Project.BLL.Mapping;

public class StudyPlanMappingProfile : Profile
{
    public StudyPlanMappingProfile()
    {
        CreateMap<StudyPlan, StudyPlanDto>();

        CreateMap<StudyPlanItem, StudyPlanItemDto>()
            .ForMember(d => d.SubjectName, o => o.MapFrom(s => s.Subject.Name));

        CreateMap<StudyPlan, StudyPlanSummaryDto>()
            .ForMember(d => d.TotalSessions, o => o.MapFrom(s => s.Items.Count(i => !i.IsDeleted)))
            .ForMember(d => d.CompletedSessions, o => o.MapFrom(s => s.Items.Count(i => !i.IsDeleted && i.IsCompleted)));

        CreateMap<GenerateStudyPlanRequest, StudyPlan>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.GeneratedByAI, o => o.MapFrom(_ => true))
            .ForMember(d => d.IsActive, o => o.MapFrom(_ => true))
            .ForMember(d => d.Items, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.Enrollment, o => o.Ignore());

        CreateMap<CreateStudyPlanItemRequest, StudyPlanItem>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.StudyPlanId, o => o.Ignore())
            .ForMember(d => d.IsCompleted, o => o.MapFrom(_ => false))
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.StudyPlan, o => o.Ignore())
            .ForMember(d => d.Subject, o => o.Ignore());
    }
}
