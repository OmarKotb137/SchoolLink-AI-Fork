using AutoMapper;
using Project.BLL.DTOs;
using Project.Domain.Entities;

namespace Project.BLL.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Academic Year
        CreateMap<AcademicYear, AcademicYearDto>();
        CreateMap<CreateAcademicYearRequest, AcademicYear>()
            .ForMember(d => d.IsCurrent, opt => opt.Ignore());

        // Grade Level
        CreateMap<GradeLevel, GradeLevelDto>();
        CreateMap<CreateGradeLevelRequest, GradeLevel>();

        // Subject
        CreateMap<Subject, SubjectDto>();
        CreateMap<CreateSubjectRequest, Subject>();

        // SchoolClass
        CreateMap<SchoolClass, ClassDto>()
            .ForMember(d => d.GradeLevelName,
                opt => opt.MapFrom(s => s.GradeLevel.Name))
            .ForMember(d => d.AcademicYearName,
                opt => opt.MapFrom(s => s.AcademicYear.Name));

        // ClassSubjectTeacher
        CreateMap<ClassSubjectTeacher, ClassSubjectTeacherDto>()
            .ForMember(d => d.ClassName,
                opt => opt.MapFrom(s => s.Class.Name))
            .ForMember(d => d.SubjectName,
                opt => opt.MapFrom(s => s.Subject.Name))
            .ForMember(d => d.TeacherName,
                opt => opt.MapFrom(s => s.Teacher.FullName))
            .ForMember(d => d.AcademicYearName,
                opt => opt.MapFrom(s => s.AcademicYear.Name));

        // Timetable
        CreateMap<Timetable, TimetableDto>()
            .ForMember(d => d.ClassName,
                opt => opt.MapFrom(s => s.Class.Name));

        // TimetableSlot
        CreateMap<TimetableSlot, TimetableSlotDto>()
            .ForMember(d => d.DayOfWeek,
                opt => opt.MapFrom(s => s.DayOfWeek.ToString()))
            .ForMember(d => d.SubjectName,
                opt => opt.MapFrom(s => s.ClassSubjectTeacher != null
                    ? s.ClassSubjectTeacher.Subject.Name : null))
            .ForMember(d => d.TeacherName,
                opt => opt.MapFrom(s => s.ClassSubjectTeacher != null
                    ? s.ClassSubjectTeacher.Teacher.FullName : null));
    }
}
