using AutoMapper;
using Project.BLL.DTOs.Assignment;
using Project.BLL.DTOs.AssignmentQuestion;
using Project.Domain.Entities;

namespace Project.BLL.Mapping
{
    public class AssignmentMappingProfile : Profile
    {
        public AssignmentMappingProfile()
        {
            // Assignment → AssignmentDto
            CreateMap<Assignment, AssignmentDto>()
                .ForMember(d => d.SubjectName, o => o.MapFrom(s => s.ClassSubjectTeacher.Subject.Name))
                .ForMember(d => d.ClassName, o => o.MapFrom(s => s.ClassSubjectTeacher.Class.Name))
                .ForMember(d => d.TeacherName, o => o.MapFrom(s => s.ClassSubjectTeacher.Teacher.FullName))
                .ForMember(d => d.Category, o => o.MapFrom(s => s.Category.ToString()))
                .ForMember(d => d.QuestionsCount, o => o.MapFrom(s => s.Questions.Count(q => !q.IsDeleted)));

            // Assignment → GetAssignmentDto
            CreateMap<Assignment, GetAssignmentDto>()
                .ForMember(d => d.SubjectName, o => o.MapFrom(s => s.ClassSubjectTeacher.Subject.Name))
                .ForMember(d => d.ClassName, o => o.MapFrom(s => s.ClassSubjectTeacher.Class.Name))
                .ForMember(d => d.TeacherName, o => o.MapFrom(s => s.ClassSubjectTeacher.Teacher.FullName))
                .ForMember(d => d.QuestionsCount, o => o.MapFrom(s => s.Questions.Count(q => !q.IsDeleted)))
                .ForMember(d => d.Questions, o => o.MapFrom(s => s.Questions.Where(q => !q.IsDeleted)));

            // CreateAssignmentDto → Assignment
            CreateMap<CreateAssignmentDto, Assignment>();

            // AssignmentQuestion → GetAssignmentQuestionDto
            CreateMap<AssignmentQuestion, GetAssignmentQuestionDto>()
                .ForMember(d => d.Options, o => o.MapFrom(s => s.Options.Where(o => !o.IsDeleted)));

            // AssignmentQuestionOption → GetAssignmentQuestionOptionDto
            CreateMap<AssignmentQuestionOption, GetAssignmentQuestionOptionDto>();

            // CreateAssignmentQuestionDto → AssignmentQuestion
            CreateMap<CreateAssignmentQuestionDto, AssignmentQuestion>()
                .ForMember(d => d.Options, o => o.MapFrom(s => s.Options));

            // CreateAssignmentQuestionOptionDto → AssignmentQuestionOption
            CreateMap<CreateAssignmentQuestionOptionDto, AssignmentQuestionOption>();

            // Assignment → AssignmentSummaryDto
            CreateMap<Assignment, AssignmentSummaryDto>()
                .ForMember(d => d.SubjectName, o => o.MapFrom(s => s.ClassSubjectTeacher.Subject.Name))
                .ForMember(d => d.ClassName, o => o.MapFrom(s => s.ClassSubjectTeacher.Class.Name))
                .ForMember(d => d.TeacherName, o => o.MapFrom(s => s.ClassSubjectTeacher.Teacher.FullName))
                .ForMember(d => d.Category, o => o.MapFrom(s => s.Category.ToString()))
                .ForMember(d => d.QuestionsCount, o => o.MapFrom(s => s.Questions.Count(q => !q.IsDeleted)));
        }
    }
}