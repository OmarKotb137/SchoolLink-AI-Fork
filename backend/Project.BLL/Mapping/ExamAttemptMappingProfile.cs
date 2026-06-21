using AutoMapper;
using Project.BLL.DTOs.ExamAttempt;
using Project.Domain.Entities;
using Project.Domain.Enums;

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
                            : 0))
                // ─────────────────────────────────────────────────────────────────
                // FIX: تبسيط منطق الـ Status
                // القاعدة:
                //   - لم يسلّم بعد              → "pending"
                //   - سلّم + تم التصحيح الكامل  → "graded"
                //   - سلّم + لم يكتمل التصحيح  → "waitingGrade"
                //     (يشمل الأسئلة المقالية وأكمل الفراغ غير المصحَّحة)
                //
                // المشكلة القديمة: كان الكود يعتمد على a.Question != null لاكتشاف
                // الأسئلة المقالية، لكن بسبب HasQueryFilter(IsDeleted) على ExamQuestion
                // كان a.Question يرجع null لو السؤال اتحذف → يُصنَّف الـ attempt خطأ
                // كـ "submitted" بدل "waitingGrade" → زرار التصحيح ما يظهرش.
                //
                // الحل: نعتمد على IsGraded فقط (يتحسب بدقة في SubmitAttemptAsync):
                //   - hasManualQuestions = true  → IsGraded = false → "waitingGrade"
                //   - hasManualQuestions = false → IsGraded = true  → "graded"
                // ─────────────────────────────────────────────────────────────────
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src =>
                    !src.SubmittedAt.HasValue ? "pending" :
                    src.IsGraded             ? "graded"  :
                                               "waitingGrade"));

            // StudentExamAnswer → GetExamAnswerDto
            CreateMap<StudentExamAnswer, GetExamAnswerDto>()
                .ForMember(dest => dest.QuestionText,
                    opt => opt.MapFrom(src => src.Question.QuestionText))
                .ForMember(dest => dest.QuestionType,
                    opt => opt.MapFrom(src =>
                        src.Question.QuestionType == QuestionType.MultipleChoice ? "mcq" :
                        src.Question.QuestionType == QuestionType.TrueFalse ? "true-false" :
                        src.Question.QuestionType == QuestionType.FillBlank ? "fill-blank" : "essay"))
                .ForMember(dest => dest.QuestionPoints,
                    opt => opt.MapFrom(src => src.Question.Points))
                .ForMember(dest => dest.AnswerText,
                    opt => opt.MapFrom(src => ResolveAnswerText(src)))
                .ForMember(dest => dest.Feedback,
                    opt => opt.MapFrom(src => src.AIFeedback))
                .ForMember(dest => dest.CorrectAnswerText,
                    opt => opt.MapFrom(src => src.Question.CorrectAnswer));
        }

        private static string? ResolveAnswerText(StudentExamAnswer src)
        {
            if (src.SelectedOptionId.HasValue && src.Question?.Options != null)
            {
                var selected = src.Question.Options.FirstOrDefault(o => o.Id == src.SelectedOptionId.Value);
                if (selected != null) return selected.OptionText;
            }
            if (src.BooleanAnswer.HasValue)
                return src.BooleanAnswer.Value ? "صح" : "خطأ";
            return src.AnswerText;
        }
    }
}
