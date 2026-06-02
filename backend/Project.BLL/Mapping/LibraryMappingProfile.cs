using AutoMapper;
using Project.BLL.DTOs.Library;
using Project.Domain.Entities;

namespace Project.BLL.Mapping;

public class LibraryMappingProfile : Profile
{
    public LibraryMappingProfile()
    {
        CreateMap<LibraryItem, LibraryItemDto>()
            .ForMember(d => d.SubjectName, o => o.MapFrom(s => s.Subject != null ? s.Subject.Name : null))
            .ForMember(d => d.GradeLevelName, o => o.MapFrom(s => s.GradeLevel != null ? s.GradeLevel.Name : null))
            .ForMember(d => d.UploadedByName, o => o.MapFrom(s => s.UploadedBy.FullName));

        CreateMap<CreateLibraryItemRequest, LibraryItem>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.UploadedBy, o => o.Ignore())
            .ForMember(d => d.Subject, o => o.Ignore())
            .ForMember(d => d.GradeLevel, o => o.Ignore())
            .ForMember(d => d.AcademicYear, o => o.Ignore())
            .ForMember(d => d.IsActive, o => o.MapFrom(_ => true));
    }
}
