using AutoMapper;
using Project.BLL.DTOs.Feedback;
using Project.Domain.Entities;

namespace Project.BLL.Mapping;

public class FeedbackMappingProfile : Profile
{
    public FeedbackMappingProfile()
    {
        CreateMap<LessonFeedback, LessonFeedbackDto>()
            .ForMember(d => d.TeacherName, o => o.MapFrom(s => s.ClassSubjectTeacher != null && s.ClassSubjectTeacher.Teacher != null ? s.ClassSubjectTeacher.Teacher.FullName : null))
            .ForMember(d => d.SubjectName, o => o.MapFrom(s => s.ClassSubjectTeacher != null && s.ClassSubjectTeacher.Subject != null ? s.ClassSubjectTeacher.Subject.Name : null));

        CreateMap<SubmitFeedbackRequest, LessonFeedback>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.Enrollment, o => o.Ignore())
            .ForMember(d => d.ClassSubjectTeacher, o => o.Ignore());
    }
}
