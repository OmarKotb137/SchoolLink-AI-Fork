using AutoMapper;
using Project.BLL.DTOs.ExamAttempt;
using Project.BLL.Utils;
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
                    opt => opt.MapFrom(src => src.Question != null ? src.Question.QuestionText : null))
                .ForMember(dest => dest.QuestionType,
                    opt => opt.MapFrom(src =>
                        src.Question == null ? "unknown" :
                        src.Question.QuestionType == QuestionType.MultipleChoice ? "mcq" :
                        src.Question.QuestionType == QuestionType.TrueFalse ? "true-false" :
                        src.Question.QuestionType == QuestionType.FillBlank ? "fill-blank" : "essay"))
                .ForMember(dest => dest.QuestionPoints,
                    opt => opt.MapFrom(src => src.Question != null ? src.Question.Points : 0))
                .ForMember(dest => dest.AnswerText,
                    opt => opt.MapFrom(src => ResolveAnswerText(src)))
                .ForMember(dest => dest.Feedback,
                    opt => opt.MapFrom(src => src.AIFeedback))
                .ForMember(dest => dest.CorrectAnswerText,
                    opt => opt.MapFrom(src => ResolveCorrectAnswerText(src)));
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

        /// <summary>
        /// يحوّل الإجابة النموذجية لصيغة عربية للعرض ("صح"/"خطأ") لأسئلة صح/خطأ،
        /// ويسيب باقي الأنواع زي ما هي. ده يمنع عرض "True"/"False" الإنجليزية للطالب/المعلم.
        /// </summary>
        private static string? ResolveCorrectAnswerText(StudentExamAnswer src)
        {
            var q = src.Question;
            if (q == null) return null;

            if (q.QuestionType == QuestionType.TrueFalse)
            {
                // الأول: لو فيه options، نعرض نص الإجابة الصحيحة من options (عشان نحافظ على الـ wording الأصلي)
                if (q.Options != null)
                {
                    var correctOpt = q.Options.FirstOrDefault(o => o.IsCorrect && !o.IsDeleted);
                    if (correctOpt != null) return correctOpt.OptionText;
                }

                // ثانياً: نطبّع القيمة النصية لـ "صح"/"خطأ"
                var normalized = BooleanNormalizer.NormalizeBoolean(q.CorrectAnswer);
                if (normalized.HasValue)
                    return normalized.Value ? "صح" : "خطأ";
            }

            // لأنواع الأسئلة الأخرى (MCQ بدون options، FillBlank، Essay) نعرض النص زي ما هو
            return q.CorrectAnswer;
        }
    }
}
