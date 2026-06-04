using AutoMapper;
using Project.BLL.DTOs.Enrollments;
using Project.Domain.Entities;

namespace Project.BLL.Mapping;

public class EnrollmentMappingProfile : Profile
{
    public EnrollmentMappingProfile()
    {
        CreateMap<StudentEnrollment, EnrollmentDto>()
            .ForMember(d => d.StudentName, o => o.MapFrom(s => s.Student != null ? s.Student.FullName : null))
            .ForMember(d => d.ClassName, o => o.MapFrom(s => s.Class != null ? s.Class.Name : null))
            .ForMember(d => d.AcademicYearName, o => o.MapFrom(s => s.AcademicYear != null ? s.AcademicYear.Name : null));

        CreateMap<EnrollStudentRequest, StudentEnrollment>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.LeftAt, o => o.Ignore())
            .ForMember(d => d.TransferReason, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.Student, o => o.Ignore())
            .ForMember(d => d.Class, o => o.Ignore())
            .ForMember(d => d.AcademicYear, o => o.Ignore());
    }
}
