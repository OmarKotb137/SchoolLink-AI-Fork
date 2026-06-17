using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.API.Services;

/// <summary>
/// خدمة خلفية بتشتغل كل 60 ثانية وتسلّم تلقائياً أي محاولة امتحان انتهى وقتها
/// (سواء عن طريق Duration الامتحان أو EndTime الكلي) ولسه متسلّمتش.
/// الإجابات بتكون محفوظة بالفعل في الـ DB عن طريق الـ Auto-Save أثناء الامتحان.
/// </summary>
public class ExamAutoSubmitService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExamAutoSubmitService> _logger;

    public ExamAutoSubmitService(IServiceScopeFactory scopeFactory, ILogger<ExamAutoSubmitService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredAttemptsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "حدث خطأ أثناء معالجة الامتحانات المنتهية تلقائياً");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // التطبيق بيتقفل — متوقع، نتجاهلها
            }
        }
    }

    private async Task ProcessExpiredAttemptsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var expiredAttempts = await unitOfWork.StudentExamAttempts.GetExpiredUnsubmittedAsync(ct);
        if (expiredAttempts.Count == 0)
            return;

        _logger.LogInformation("تم العثور على {Count} محاولة امتحان منتهية الوقت — جاري التسليم التلقائي", expiredAttempts.Count);

        foreach (var expired in expiredAttempts)
        {
            try
            {
                await SubmitExpiredAttemptAsync(unitOfWork, expired.Id, expired.EnrollmentId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "فشل التسليم التلقائي للمحاولة {AttemptId}", expired.Id);
            }
        }
    }

    private async Task SubmitExpiredAttemptAsync(IUnitOfWork unitOfWork, int attemptId, int enrollmentId, CancellationToken ct)
    {
        // إعادة الجلب بكل الـ Includes اللازمة للتصحيح (الأسئلة + الاختيارات + الإجابات المحفوظة)
        var attempt = await unitOfWork.StudentExamAttempts.GetWithAnswersForEnrollmentAsync(attemptId, enrollmentId, ct);
        if (attempt == null || attempt.SubmittedAt.HasValue)
            return; // اتسلّمت بالفعل (مثلاً الطالب سلّمها يدوياً قبل ما الخدمة توصلها)

        var savedAnswersByQuestion = attempt.Answers.ToDictionary(a => a.QuestionId);

        decimal totalScore = 0;
        var hasManualQuestions = false;

        foreach (var question in attempt.Exam.Questions.Where(q => !q.IsDeleted))
        {
            if (savedAnswersByQuestion.TryGetValue(question.Id, out var existingAnswer))
            {
                GradeObjectiveAnswer(question, existingAnswer);

                if (existingAnswer.IsCorrect.HasValue)
                    totalScore += existingAnswer.PointsEarned;
                else
                    hasManualQuestions = true;

                unitOfWork.StudentExamAnswers.Update(existingAnswer);
            }
            else
            {
                // الطالب ملوش إجابة محفوظة لهذا السؤال — يُسجَّل بدون إجابة (null)
                var answer = new StudentExamAnswer
                {
                    AttemptId = attempt.Id,
                    QuestionId = question.Id,
                    AnswerText = null,
                    SelectedOptionId = null,
                    BooleanAnswer = null
                };

                GradeObjectiveAnswer(question, answer);

                if (answer.IsCorrect.HasValue)
                    totalScore += answer.PointsEarned;
                else
                    hasManualQuestions = true;

                await unitOfWork.StudentExamAnswers.AddAsync(answer, ct);
            }
        }

        attempt.SubmittedAt = DateTime.UtcNow;
        attempt.Score = totalScore;
        attempt.IsGraded = !hasManualQuestions;
        attempt.UpdatedAt = DateTime.UtcNow;

        unitOfWork.StudentExamAttempts.Update(attempt);
        await unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("تم تسليم المحاولة {AttemptId} تلقائياً بعد انتهاء الوقت (الدرجة: {Score})", attempt.Id, totalScore);
    }

    // منطق التصحيح نفسه المستخدم في StudentExamService.SubmitAttemptAsync —
    // مكرر هنا عمداً لأن الـ Background Service مش بيعتمد على الـ Service Layer
    // (مفيش "userId" حالي في سياق التشغيل التلقائي).
    private static void GradeObjectiveAnswer(ExamQuestion question, StudentExamAnswer answer)
    {
        if (question.QuestionType == QuestionType.MultipleChoice)
        {
            var selectedOption = question.Options.FirstOrDefault(o => o.Id == answer.SelectedOptionId && !o.IsDeleted);
            var isCorrect = selectedOption?.IsCorrect == true;
            answer.IsCorrect = isCorrect;
            answer.PointsEarned = isCorrect ? question.Points : 0;
            return;
        }

        if (question.QuestionType == QuestionType.TrueFalse)
        {
            var correct = NormalizeBoolean(question.CorrectAnswer);
            var isCorrect = correct.HasValue && answer.BooleanAnswer.HasValue && correct.Value == answer.BooleanAnswer.Value;
            answer.IsCorrect = isCorrect;
            answer.PointsEarned = isCorrect ? question.Points : 0;
        }
    }

    private static bool? NormalizeBoolean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim().ToLowerInvariant();
        return normalized switch
        {
            "true" or "صح" or "صحيح" or "1" => true,
            "false" or "خطأ" or "خطا" or "0" => false,
            _ => null
        };
    }
}
