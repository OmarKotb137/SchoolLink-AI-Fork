using System.Text.Json;
using Common.Results;
using Microsoft.Extensions.Logging;
using Project.BLL.AI.ExamAgent.Interfaces;
using Project.BLL.AI.ExamAgent.Models;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;
using Project.BLL.AI.Tools;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;

namespace Project.BLL.AI.Agents;

public class ParentAssistantAgent : IParentAssistantAgent
{
    private readonly ILLMRouter _router;
    private readonly ILlmClient _llmClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IParentStudentService _parentStudentService;
    private readonly IStudentEvaluationService _evalService;
    private readonly IPeriodicAssessmentService _periodicService;
    private readonly IExamService _examService;
    private readonly IPeriodAverageService _periodAverageService;
    private readonly ILogger<ParentAssistantAgent> _logger;
    private readonly IAgentChatStore _chatStore;

    private const string SystemPrompt =
        "أنت مساعد ذكي لولي الأمر. قدم تقارير واضحة عن مستوى الطالب الأكاديمي والسلوكي. " +
        "اقترح أنشطة تعليمية منزلية وحدد نقاط القوة والضعف. كن موضوعياً ومشجعاً. " +
        "لديك أدوات يمكنك استخدامها لجلب معلومات حقيقية من النظام. استخدمها عند الحاجة.";

    public ParentAssistantAgent(
        ILLMRouter router,
        ILlmClient llmClient,
        IToolRegistry toolRegistry,
        IUnitOfWork unitOfWork,
        IParentStudentService parentStudentService,
        IStudentEvaluationService evalService,
        IPeriodicAssessmentService periodicService,
        IExamService examService,
        IPeriodAverageService periodAverageService,
        ILogger<ParentAssistantAgent> logger,
        IAgentChatStore chatStore)
    {
        _router = router;
        _llmClient = llmClient;
        _unitOfWork = unitOfWork;
        _parentStudentService = parentStudentService;
        _evalService = evalService;
        _periodicService = periodicService;
        _examService = examService;
        _periodAverageService = periodAverageService;
        _logger = logger;
        _chatStore = chatStore;
        toolRegistry.RegisterTools(ParentTools.Create(parentStudentService, evalService, periodicService, examService, periodAverageService));
    }

    public async Task<OperationResult<AgentResponse>> ChatAsync(string message, string? conversationId = null, UserContext? context = null, CancellationToken ct = default)
    {
        conversationId ??= Guid.NewGuid().ToString();
        context ??= new UserContext();

        await ResolveParentContextAsync(context, ct);

        var messages = new List<LlmChatMessage>
        {
            new(MessageRole.System, SystemPrompt)
        };

        var history = await _chatStore.GetRecentMessagesAsync(conversationId, 10, ct);
        foreach (var msg in history)
            messages.Add(new LlmChatMessage(
                msg.Role == "user" ? MessageRole.User : MessageRole.Assistant, msg.Content));

        messages.Add(new LlmChatMessage(MessageRole.User, message));
        await _chatStore.SaveMessageAsync(conversationId, "user", message, "parent", ct);

        var tools = CreateTools(context);
        var toolDefs = tools.Values.Select(t => new FunctionDefinition
        {
            Name = t.Name,
            Description = t.Description,
            InputSchema = t.Parameters ?? System.Text.Json.JsonDocument.Parse("{}").RootElement
        }).ToList();

        for (int step = 0; step < 10; step++)
        {
            _logger.LogInformation("ParentAgent step {Step} for conv {ConvId}", step + 1, conversationId);

            var response = await _llmClient.ChatAsync(messages, toolDefs);

            if (response.ToolCalls is null || response.ToolCalls.Count == 0)
            {
                var answer = response.Content ?? "لم يتمكن المساعد من الإجابة.";
                await _chatStore.SaveMessageAsync(conversationId, "assistant", answer, "parent", ct);
                return OperationResult<AgentResponse>.Success(new AgentResponse
                {
                    Text = answer,
                    SuggestedActions = new() { "عرض تقارير سابقة", "جدولة اجتماع", "أنشطة مقترحة" },
                    AdditionalData = new() { ["conversationId"] = conversationId }
                });
            }

            messages.Add(new LlmChatMessage(
                MessageRole.Assistant,
                response.Content ?? "",
                toolCalls: response.ToolCalls));

            foreach (var call in response.ToolCalls)
            {
                _logger.LogInformation("ParentAgent calling tool: {Tool}", call.Name);

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
                    _logger.LogError(ex, "ParentAgent tool {Tool} failed", call.Name);
                    messages.Add(new LlmChatMessage(MessageRole.Tool,
                        $"{{\"error\": \"{ex.Message}\"}}", toolCallId: call.Id));
                }
            }
        }

        _logger.LogError("ParentAgent exceeded max steps for conv {ConvId}", conversationId);
        return OperationResult<AgentResponse>.Success(new AgentResponse
        {
            Text = "عذراً، لم أتمكن من إكمال العملية. يرجى إعادة صياغة سؤالك.",
            AdditionalData = new() { ["conversationId"] = conversationId }
        });
    }

    private async Task ResolveParentContextAsync(UserContext context, CancellationToken ct)
    {
        context.ParentId ??= context.UserId;

        var activeYear = await _unitOfWork.AcademicYears.FindAsync(y => y.IsCurrent && !y.IsDeleted);
        context.AcademicYearId = activeYear.FirstOrDefault()?.Id;
    }

    private Dictionary<string, AiTool> CreateTools(UserContext context)
    {
        var list = new List<AiTool>();

        if (context.ParentId.HasValue)
        {
            var pId = context.ParentId.Value;
            list.Add(new AiTool
            {
                Name = "get_my_children",
                Description = "جلب قائمة أبناء ولي الأمر",
                Parameters = JsonSerializer.SerializeToElement(new
                {
                    type = "object",
                    properties = new object(),
                    required = Array.Empty<string>()
                }),
                ExecuteAsync = async (args) =>
                {
                    var result = await _parentStudentService.GetStudentsByParentAsync(pId);
                    return JsonSerializer.Serialize(result.Data, new JsonSerializerOptions { WriteIndented = true });
                }
            });
        }

        list.Add(new AiTool
        {
            Name = "get_child_evaluations",
            Description = "جلب تقييمات الابن (دراسية + تدريبية)",
            Parameters = JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    enrollmentId = new { type = "integer", description = "معرف تسجيل الطالب" },
                    periodId = new { type = "integer", description = "معرف فترة التقييم (اختياري)" }
                },
                required = new[] { "enrollmentId" }
            }),
            ExecuteAsync = async (args) =>
            {
                using var doc = JsonDocument.Parse(args);
                var eId = doc.RootElement.GetProperty("enrollmentId").GetInt32();
                var pId = doc.RootElement.TryGetProperty("periodId", out var periodEl) ? periodEl.GetInt32() : 0;

                var academic = await _evalService.GetByEnrollmentAndPeriodAsync(eId, pId);
                var training = await _periodicService.GetByEnrollmentAsync(eId);
                return JsonSerializer.Serialize(new
                {
                    academicEvaluations = academic.Data,
                    trainingAssessments = training.Data
                }, new JsonSerializerOptions { WriteIndented = true });
            }
        });

        list.Add(new AiTool
        {
            Name = "get_child_performance_trend",
            Description = "جلب مؤشرات الأداء عبر الفترات لكشف الانخفاض أو التحسن",
            Parameters = JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    enrollmentId = new { type = "integer", description = "معرف تسجيل الطالب" }
                },
                required = new[] { "enrollmentId" }
            }),
            ExecuteAsync = async (args) =>
            {
                using var doc = JsonDocument.Parse(args);
                var eId = doc.RootElement.GetProperty("enrollmentId").GetInt32();
                var result = await _periodAverageService.GetByEnrollmentAsync(eId);
                return JsonSerializer.Serialize(result.Data, new JsonSerializerOptions { WriteIndented = true });
            }
        });

        if (context.AcademicYearId.HasValue)
        {
            var yId = context.AcademicYearId.Value;
            list.Add(new AiTool
            {
                Name = "get_child_upcoming_exams",
                Description = "جلب الامتحانات القادمة للابن",
                Parameters = JsonSerializer.SerializeToElement(new
                {
                    type = "object",
                    properties = new
                    {
                        classId = new { type = "integer", description = "معرف الفصل" }
                    },
                    required = new[] { "classId" }
                }),
                ExecuteAsync = async (args) =>
                {
                    using var doc = JsonDocument.Parse(args);
                    var cId = doc.RootElement.GetProperty("classId").GetInt32();
                    var result = await _examService.GetUpcomingExamsAsync(cId, yId);
                    return JsonSerializer.Serialize(result.Data, new JsonSerializerOptions { WriteIndented = true });
                }
            });
        }

        return list.ToDictionary(t => t.Name);
    }

    public async Task<OperationResult<AgentResponse>> GetProgressSummaryAsync(int studentId, CancellationToken ct = default)
    {
        var student = await _unitOfWork.Students.GetByIdAsync(studentId);
        if (student == null || student.IsDeleted)
            return OperationResult<AgentResponse>.Failure("الطالب غير موجود", 404);

        var enrollments = await _unitOfWork.StudentEnrollments.FindAsync(e => e.StudentId == studentId && !e.IsDeleted);
        var enrollment = enrollments.FirstOrDefault();
        if (enrollment == null)
            return OperationResult<AgentResponse>.Failure("الطالب غير مسجل في أي فصل", 404);

        var evaluations = await _unitOfWork.StudentEvaluations.FindAsync(e => e.EnrollmentId == enrollment.Id && !e.IsDeleted);
        var absences = await _unitOfWork.DailyAbsences.FindAsync(a => a.EnrollmentId == enrollment.Id && !a.IsDeleted);

        var summary = $"الطالب: {student.User?.FullName ?? "غير معروف"}\nعدد التقييمات: {evaluations.Count}\nعدد مرات الغياب: {absences.Count}";

        var result = await _router.GenerateAsync(SystemPrompt,
            $"قدّم تقريراً عن تقدم الطالب بناءً على:\n{summary}", ct: ct);

        return OperationResult<AgentResponse>.Success(new AgentResponse
        {
            Text = result,
            SuggestedActions = new() { "عرض تقارير سابقة", "جدولة اجتماع", "أنشطة مقترحة" }
        });
    }

    public async Task<OperationResult<AgentResponse>> SuggestLearningActivitiesAsync(int studentId, CancellationToken ct = default)
    {
        var prompt = $"اقترح أنشطة تعليمية منزلية مناسبة لطالب (ID: {studentId}) في المواد المختلفة.";
        var result = await _router.GenerateAsync(SystemPrompt, prompt, ct: ct);
        return OperationResult<AgentResponse>.Success(new AgentResponse { Text = result });
    }

    public async Task<OperationResult<AgentResponse>> IdentifyWeakAreasAsync(int studentId, CancellationToken ct = default)
    {
        var prompt = $"حلل أداء الطالب (ID: {studentId}) وحدد نقاط الضعف الأكاديمية واقترح خطة تحسين.";
        var result = await _router.GenerateAsync(SystemPrompt, prompt, ct: ct);
        return OperationResult<AgentResponse>.Success(new AgentResponse { Text = result });
    }

    public async Task<OperationResult<AgentResponse>> RecommendResourcesAsync(string subject, string gradeLevel, CancellationToken ct = default)
    {
        var prompt = $"أوصِ بمصادر تعليمية (كتب، فيديوهات، تطبيقات) لمادة '{subject}' للصف '{gradeLevel}'.";
        var result = await _router.GenerateAsync(SystemPrompt, prompt, ct: ct);
        return OperationResult<AgentResponse>.Success(new AgentResponse { Text = result });
    }
}
