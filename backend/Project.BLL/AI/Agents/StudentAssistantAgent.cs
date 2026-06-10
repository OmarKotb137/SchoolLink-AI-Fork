using System.Text.Json;
using Common.Results;
using Microsoft.Extensions.Logging;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;
using Project.BLL.AI.Tools;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;

namespace Project.BLL.AI.Agents;

public class StudentAssistantAgent : IStudentAssistantAgent
{
    private readonly ILLMRouter _router;
    private readonly ILlmClient _llmClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<StudentAssistantAgent> _logger;
    private readonly IAgentChatStore _chatStore;
    private readonly ILessonRepository _lessonRepo;
    private readonly IStudentEvaluationService _evalService;
    private readonly IPeriodicAssessmentService _periodicService;
    private readonly IExamService _examService;

    private const string SystemPrompt =
        "أنت مساعد تعليمي ذكي للطالب. أجب عن الأسئلة التعليمية بوضوح وباللغة العربية. " +
        "استخدم أسلوباً تعليمياً مبسطاً يناسب مستوى الطالب. قدم أمثلة وتطبيقات عملية. " +
        "إذا سأل الطالب عن مسألة رياضية، اشرح خطوات الحل بالتفصيل قبل إعطاء الإجابة النهائية. " +
        "لديك أدوات يمكنك استخدامها لجلب معلومات حقيقية من النظام. استخدمها عند الحاجة.";

    public StudentAssistantAgent(
        ILLMRouter router,
        ILlmClient llmClient,
        IToolRegistry toolRegistry,
        IUnitOfWork unitOfWork,
        ILessonRepository lessonRepo,
        IStudentEvaluationService evalService,
        IPeriodicAssessmentService periodicService,
        IExamService examService,
        ILogger<StudentAssistantAgent> logger,
        IAgentChatStore chatStore)
    {
        _router = router;
        _llmClient = llmClient;
        _unitOfWork = unitOfWork;
        _lessonRepo = lessonRepo;
        _evalService = evalService;
        _periodicService = periodicService;
        _examService = examService;
        _logger = logger;
        _chatStore = chatStore;
        toolRegistry.RegisterTools(StudentTools.Create(lessonRepo, evalService, periodicService, examService));
    }

    public async Task<OperationResult<AgentResponse>> ChatAsync(string message, string? conversationId = null, UserContext? context = null, CancellationToken ct = default)
    {
        conversationId ??= Guid.NewGuid().ToString();
        context ??= new UserContext();

        var enrollment = await ResolveStudentContextAsync(context, ct);

        var messages = new List<LlmChatMessage>
        {
            new(MessageRole.System, SystemPrompt)
        };

        var history = await _chatStore.GetRecentMessagesAsync(conversationId, 10, ct);
        foreach (var msg in history)
            messages.Add(new LlmChatMessage(
                msg.Role == "user" ? MessageRole.User : MessageRole.Assistant, msg.Content));

        messages.Add(new LlmChatMessage(MessageRole.User, message));
        await _chatStore.SaveMessageAsync(conversationId, "user", message, "student", ct);

        var tools = CreateTools(context, enrollment);
        var toolDefs = tools.Values.Select(t => new FunctionDefinition
        {
            Name = t.Name,
            Description = t.Description,
            InputSchema = t.Parameters ?? System.Text.Json.JsonDocument.Parse("{}").RootElement
        }).ToList();

        for (int step = 0; step < 10; step++)
        {
            _logger.LogInformation("StudentAgent step {Step} for conv {ConvId}", step + 1, conversationId);

            var response = await _llmClient.ChatAsync(messages, toolDefs);

            if (response.ToolCalls is null || response.ToolCalls.Count == 0)
            {
                var answer = response.Content ?? "لم يتمكن المساعد من الإجابة.";
                await _chatStore.SaveMessageAsync(conversationId, "assistant", answer, "student", ct);
                return OperationResult<AgentResponse>.Success(new AgentResponse
                {
                    Text = answer,
                    SuggestedActions = new() { "اطرح سؤالاً متابعة", "اطلب شرح مفهوم آخر", "حل تمرين مشابه" },
                    AdditionalData = new() { ["conversationId"] = conversationId }
                });
            }

            messages.Add(new LlmChatMessage(
                MessageRole.Assistant,
                response.Content ?? "",
                toolCalls: response.ToolCalls));

            foreach (var call in response.ToolCalls)
            {
                _logger.LogInformation("StudentAgent calling tool: {Tool}", call.Name);

                if (!tools.TryGetValue(call.Name, out var tool))
                {
                    messages.Add(new LlmChatMessage(MessageRole.Tool,
                        $"{{\"error\": \"الأداة '{call.Name}' غير موجودة\"}}", toolCallId: call.Id));
                    continue;
                }

                try
                {
                    var result = await tool.ExecuteAsync(call.Arguments);
                    messages.Add(new LlmChatMessage(MessageRole.Tool, result, toolCallId: call.Id));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "StudentAgent tool {Tool} failed", call.Name);
                    messages.Add(new LlmChatMessage(MessageRole.Tool,
                        $"{{\"error\": \"{ex.Message}\"}}", toolCallId: call.Id));
                }
            }
        }

        _logger.LogError("StudentAgent exceeded max steps for conv {ConvId}", conversationId);
        return OperationResult<AgentResponse>.Success(new AgentResponse
        {
            Text = "عذراً، لم أتمكن من إكمال العملية. يرجى إعادة صياغة سؤالك.",
            AdditionalData = new() { ["conversationId"] = conversationId }
        });
    }

    private async Task<Domain.Entities.StudentEnrollment?> ResolveStudentContextAsync(UserContext context, CancellationToken ct)
    {
        if (context.EnrollmentId.HasValue)
        {
            var enrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(context.EnrollmentId.Value);
            if (enrollment != null)
            {
                context.ClassId ??= enrollment.ClassId;
                context.StudentId ??= enrollment.StudentId;
                return enrollment;
            }
        }

        var student = (await _unitOfWork.Students.FindAsync(s => s.UserId == context.UserId && !s.IsDeleted)).FirstOrDefault();
        if (student == null) return null;

        context.StudentId = student.Id;

        var enrollments = await _unitOfWork.StudentEnrollments.FindAsync(
            e => e.StudentId == student.Id && !e.IsDeleted);
        var enrollmentFirst = enrollments.FirstOrDefault();
        if (enrollmentFirst != null)
        {
            context.EnrollmentId = enrollmentFirst.Id;
            context.ClassId = enrollmentFirst.ClassId;
        }

        var activeYear = await _unitOfWork.AcademicYears.FindAsync(y => y.IsCurrent && !y.IsDeleted);
        context.AcademicYearId = activeYear.FirstOrDefault()?.Id;

        return enrollmentFirst;
    }

    private Dictionary<string, AiTool> CreateTools(UserContext context, Domain.Entities.StudentEnrollment? enrollment)
    {
        var list = new List<AiTool>();

        list.Add(new AiTool
        {
            Name = "get_lesson_content",
            Description = "جلب محتوى الدرس المطلوب حسب معرف الدرس",
            Parameters = JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    lessonId = new { type = "integer", description = "معرف الدرس الرقمي" }
                },
                required = new[] { "lessonId" }
            }),
            ExecuteAsync = async (args) =>
            {
                using var doc = JsonDocument.Parse(args);
                var id = doc.RootElement.GetProperty("lessonId").GetInt32();
                var lesson = await _lessonRepo.GetByIdAsync(id);
                return JsonSerializer.Serialize(lesson, new JsonSerializerOptions { WriteIndented = true });
            }
        });

        if (context.EnrollmentId.HasValue)
        {
            var eId = context.EnrollmentId.Value;
            list.Add(new AiTool
            {
                Name = "get_academic_evaluations",
                Description = "جلب التقييمات الدراسية للطالب (غياب، سلوك، واجبات، تفاعل)",
                Parameters = JsonSerializer.SerializeToElement(new
                {
                    type = "object",
                    properties = new
                    {
                        periodId = new { type = "integer", description = "معرف فترة التقييم" }
                    },
                    required = new[] { "periodId" }
                }),
                ExecuteAsync = async (args) =>
                {
                    using var doc = JsonDocument.Parse(args);
                    var pId = doc.RootElement.GetProperty("periodId").GetInt32();
                    var result = await _evalService.GetByEnrollmentAndPeriodAsync(eId, pId);
                    return JsonSerializer.Serialize(result.Data, new JsonSerializerOptions { WriteIndented = true });
                }
            });

            list.Add(new AiTool
            {
                Name = "get_training_assessments",
                Description = "جلب نتائج الامتحانات والواجبات للطالب",
                Parameters = JsonSerializer.SerializeToElement(new
                {
                    type = "object",
                    properties = new object(),
                    required = Array.Empty<string>()
                }),
                ExecuteAsync = async (args) =>
                {
                    var result = await _periodicService.GetByEnrollmentAsync(eId);
                    return JsonSerializer.Serialize(result.Data, new JsonSerializerOptions { WriteIndented = true });
                }
            });
        }

        if (context.ClassId.HasValue && context.AcademicYearId.HasValue)
        {
            var cId = context.ClassId.Value;
            var yId = context.AcademicYearId.Value;
            list.Add(new AiTool
            {
                Name = "get_upcoming_exams",
                Description = "جلب الامتحانات القادمة للطالب",
                Parameters = JsonSerializer.SerializeToElement(new
                {
                    type = "object",
                    properties = new object(),
                    required = Array.Empty<string>()
                }),
                ExecuteAsync = async (args) =>
                {
                    var result = await _examService.GetUpcomingExamsAsync(cId, yId);
                    return JsonSerializer.Serialize(result.Data, new JsonSerializerOptions { WriteIndented = true });
                }
            });
        }

        return list.ToDictionary(t => t.Name);
    }

    public async Task<OperationResult<AgentResponse>> AnswerQuestionAsync(AiQuestionRequest request, CancellationToken ct = default)
    {
        var prompt = $"المادة: {request.Subject}\nالموضوع: {request.Topic}\nالصف: {request.GradeLevel}\nالمستوى: {request.Difficulty}\n\nالسؤال: {request.QuestionText}";
        var result = await _router.GenerateAsync(SystemPrompt, prompt, ct: ct);
        return OperationResult<AgentResponse>.Success(new AgentResponse
        {
            Text = result,
            SuggestedActions = new() { "اطرح سؤالاً متابعة", "اطلب شرح مفهوم آخر", "حل تمرين مشابه" }
        });
    }

    public async Task<OperationResult<AgentResponse>> ExplainConceptAsync(string concept, string subject, string gradeLevel, CancellationToken ct = default)
    {
        var prompt = $"اشرح مفهوم '{concept}' في مادة '{subject}' لطالب في {gradeLevel}. استخدم أمثلة من الحياة اليومية.";
        var result = await _router.GenerateAsync(SystemPrompt, prompt, ct: ct);
        return OperationResult<AgentResponse>.Success(new AgentResponse { Text = result });
    }

    public async Task<OperationResult<AgentResponse>> GeneratePracticeExerciseAsync(string subject, string topic, int count = 5, CancellationToken ct = default)
    {
        var prompt = $"ولّد {count} تمرين تدريبي في مادة '{subject}' عن موضوع '{topic}' مع نموذج الإجابة.";
        var result = await _router.GenerateAsync(SystemPrompt, prompt, ct: ct);
        return OperationResult<AgentResponse>.Success(new AgentResponse { Text = result });
    }

    public async Task<OperationResult<AgentResponse>> AnalyzeAnswerAsync(string questionText, string studentAnswer, string? modelAnswer = null, CancellationToken ct = default)
    {
        var prompt = $"السؤال: {questionText}\nإجابة الطالب: {studentAnswer}";
        if (!string.IsNullOrEmpty(modelAnswer))
            prompt += $"\nالإجابة النموذجية: {modelAnswer}";

        prompt += "\n\nحلّل الإجابة وقدم تغذية راجعة مفصلة.";
        var result = await _router.GenerateAsync(SystemPrompt, prompt, ct: ct);
        return OperationResult<AgentResponse>.Success(new AgentResponse { Text = result });
    }
}
