using AutoMapper;
using Project.BLL.DTOs.Exam;
using Project.BLL.DTOs.ExamAttempt;
using Project.Domain.Entities;

namespace Project.BLL.Mapping
{
    public class ExamMappingProfile : Profile
    {
        public ExamMappingProfile()
        {
            CreateMap<Exam, ExamSummaryDto>()
                .ForMember(d => d.SubjectName, o => o.MapFrom(s =>
                    s.ClassSubjectTeacher != null ? s.ClassSubjectTeacher.Subject.Name :
                    s.Subject != null ? s.Subject.Name : ""))
                .ForMember(d => d.GradeLevelName, o => o.MapFrom(s =>
                    s.GradeLevel != null ? s.GradeLevel.Name : ""))
                .ForMember(d => d.QuestionsCount, o => o.MapFrom(s =>
                    s.Questions.Count(q => !q.IsDeleted) +
                    s.Groups.Sum(g => g.Questions.Count(q => !q.IsDeleted))));

            CreateMap<Exam, GetExamDto>()
                .ForMember(d => d.SubjectName, o => o.MapFrom(s =>
                    s.ClassSubjectTeacher != null ? s.ClassSubjectTeacher.Subject.Name :
                    s.Subject != null ? s.Subject.Name : ""))
                .ForMember(d => d.GradeLevelName, o => o.MapFrom(s =>
                    s.GradeLevel != null ? s.GradeLevel.Name : ""))
                .ForMember(d => d.ClassName, o => o.MapFrom(s =>
                    s.ClassSubjectTeacher != null ? s.ClassSubjectTeacher.Class.Name : ""))
                .ForMember(d => d.TeacherName, o => o.MapFrom(s =>
                    s.ClassSubjectTeacher != null ? s.ClassSubjectTeacher.Teacher.FullName : ""))
                .ForMember(d => d.QuestionsCount, o => o.MapFrom(s =>
                    s.Questions.Count(q => !q.IsDeleted) +
                    s.Groups.Sum(g => g.Questions.Count(q => !q.IsDeleted))))
                .ForMember(d => d.Groups, o => o.MapFrom(s => s.Groups.Where(g => !g.IsDeleted)))
                .ForMember(d => d.StandaloneQuestions, o => o.MapFrom(s =>
                    s.Questions.Where(q => q.GroupId == null && !q.IsDeleted)));

            CreateMap<ExamQuestionGroup, GetExamQuestionGroupDto>()
                .ForMember(d => d.Questions, o => o.MapFrom(s => s.Questions.Where(q => !q.IsDeleted)));

            CreateMap<ExamQuestion, GetExamQuestionDto>()
                .ForMember(d => d.Options, o => o.MapFrom(s => s.Options.Where(o => !o.IsDeleted)));

            CreateMap<ExamQuestionOption, GetExamQuestionOptionDto>();

            CreateMap<StudentExamAttempt, GetExamAttemptDto>()
                .ForMember(d => d.StudentName, o => o.MapFrom(s => s.Enrollment.Student.User.FullName))
                .ForMember(d => d.ExamTitle, o => o.MapFrom(s => s.Exam.Title))
                .ForMember(d => d.Answers, o => o.MapFrom(s => s.Answers));

            CreateMap<StudentExamAttempt, ExamAttemptSummaryDto>()
                .ForMember(d => d.StudentName, o => o.MapFrom(s => s.Enrollment.Student.User.FullName))
                .ForMember(d => d.TimeTakenMinutes, o => o.MapFrom(s =>
                    s.SubmittedAt.HasValue
                        ? (int)(s.SubmittedAt.Value - s.StartedAt).TotalMinutes
                        : 0));

            CreateMap<StudentExamAnswer, GetExamAnswerDto>()
                .ForMember(d => d.QuestionText, o => o.MapFrom(s => s.Question.QuestionText));
        }
    }
}
