using AutoMapper;
using Project.BLL.DTOs.ExamAttempt;
using Project.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.BLL.Mapping
{
    public class ExamAttemptMappingProfile : Profile
    {
        public ExamAttemptMappingProfile()
        {
            // StudentExamAttempt → GetExamAttemptDto
            CreateMap<StudentExamAttempt, GetExamAttemptDto>()
                .ForMember(dest => dest.StudentName,
                    opt => opt.MapFrom(src => src.Enrollment.Student.User.FullName))
                .ForMember(dest => dest.ExamTitle,
                    opt => opt.MapFrom(src => src.Exam.Title))
                .ForMember(dest => dest.Answers,
                    opt => opt.MapFrom(src => src.Answers));

            // StudentExamAttempt → ExamAttemptSummaryDto
            CreateMap<StudentExamAttempt, ExamAttemptSummaryDto>()
                .ForMember(dest => dest.StudentName,
                    opt => opt.MapFrom(src => src.Enrollment.Student.User.FullName))
                .ForMember(dest => dest.TimeTakenMinutes,
                    opt => opt.MapFrom(src =>
                        src.SubmittedAt.HasValue
                            ? (int)(src.SubmittedAt.Value - src.StartedAt).TotalMinutes
                            : 0));

            // StudentExamAnswer → GetExamAnswerDto
            // Id, AnswerText, IsCorrect, PointsEarned, AIFeedback → بيتمابوا تلقائياً
            CreateMap<StudentExamAnswer, GetExamAnswerDto>()
                .ForMember(dest => dest.QuestionText,
                    opt => opt.MapFrom(src => src.Question.QuestionText));
        }
    }
}