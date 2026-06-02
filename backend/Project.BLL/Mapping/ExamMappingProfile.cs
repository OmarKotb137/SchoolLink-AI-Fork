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
            // Exam → GetExamDto
            CreateMap<Exam, GetExamDto>()
                .ForMember(d => d.SubjectName, o => o.MapFrom(s => s.ClassSubjectTeacher.Subject.Name))
                .ForMember(d => d.ClassName, o => o.MapFrom(s => s.ClassSubjectTeacher.Class.Name))
                .ForMember(d => d.TeacherName, o => o.MapFrom(s => s.ClassSubjectTeacher.Teacher.FullName))
                .ForMember(d => d.QuestionsCount, o => o.MapFrom(s => s.Questions.Count(q => !q.IsDeleted)))
                .ForMember(d => d.Questions, o => o.MapFrom(s => s.Questions.Where(q => !q.IsDeleted)));

            // ExamQuestion → GetExamQuestionDto
            CreateMap<ExamQuestion, GetExamQuestionDto>()
                .ForMember(d => d.Options, o => o.MapFrom(s => s.Options.Where(o => !o.IsDeleted)));

            // ExamQuestionOption → GetExamQuestionOptionDto
            CreateMap<ExamQuestionOption, GetExamQuestionOptionDto>();

            // StudentExamAttempt → GetExamAttemptDto
            CreateMap<StudentExamAttempt, GetExamAttemptDto>()
                .ForMember(d => d.StudentName, o => o.MapFrom(s => s.Enrollment.Student.User.FullName))
                .ForMember(d => d.ExamTitle, o => o.MapFrom(s => s.Exam.Title))
                .ForMember(d => d.Answers, o => o.MapFrom(s => s.Answers));

            // StudentExamAttempt → ExamAttemptSummaryDto
            // StudentExamAttempt → ExamAttemptSummaryDto
            CreateMap<StudentExamAttempt, ExamAttemptSummaryDto>()
                .ForMember(d => d.StudentName, o => o.MapFrom(s => s.Enrollment.Student.User.FullName))
                .ForMember(d => d.TimeTakenMinutes, o => o.MapFrom(s =>
                    s.SubmittedAt.HasValue
                        ? (int)(s.SubmittedAt.Value - s.StartedAt).TotalMinutes
                        : 0));

            // StudentExamAnswer → GetExamAnswerDto
            CreateMap<StudentExamAnswer, GetExamAnswerDto>()
                .ForMember(d => d.QuestionText, o => o.MapFrom(s => s.Question.QuestionText));
        }
    }
}