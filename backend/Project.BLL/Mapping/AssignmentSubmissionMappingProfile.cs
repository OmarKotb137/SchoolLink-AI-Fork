using AutoMapper;
using Project.BLL.DTOs.AssignmentSubmission;
using Project.Domain.Entities;

namespace Project.BLL.Mapping
{
    public class AssignmentSubmissionMappingProfile : Profile
    {
        public AssignmentSubmissionMappingProfile()
        {
            // StudentAssignmentSubmission → GetAssignmentSubmissionDto
            CreateMap<StudentAssignmentSubmission, GetAssignmentSubmissionDto>()
                .ForMember(d => d.StudentName, o => o.MapFrom(s =>
                    s.Enrollment.Student.FullName))
                .ForMember(d => d.AssignmentTitle, o => o.MapFrom(s =>
                    s.Assignment.Title))
                .ForMember(d => d.Answers, o => o.MapFrom(s => s.Answers));

            // StudentAssignmentSubmission → AssignmentSubmissionSummaryDto
            CreateMap<StudentAssignmentSubmission, AssignmentSubmissionSummaryDto>()
                .ForMember(d => d.StudentName, o => o.MapFrom(s =>
                    s.Enrollment.Student.FullName));

            // StudentAssignmentAnswer → GetAssignmentAnswerDto
            CreateMap<StudentAssignmentAnswer, GetAssignmentAnswerDto>()
                .ForMember(d => d.QuestionText, o => o.MapFrom(s =>
                    s.Question.QuestionText));
        }
    }
}