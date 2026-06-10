using System.Text.Json;
using Common.Results;
using Microsoft.Extensions.Logging;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;
using Project.BLL.AI.Tools;
using Project.BLL.DTOs.Exam;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;

namespace Project.BLL.AI.Agents;

public class TeacherAssistantAgent : ITeacherAssistantAgent
{
    private readonly ILLMRouter _router;
    private readonly ILlmClient _llmClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISubjectService _subjectService;
    private readonly ILessonRepository _lessonRepo;
    private readonly IExamGenerator _examGenerator;
    private readonly IExamGeneratorService _examGeneratorService;
    private readonly IExamService _examService;
    private readonly ILogger<TeacherAssistantAgent> _logger;
    private readonly IAgentChatStore _chatStore;

    private const string SystemPrompt =
        "أنت مساعد تعليمي ذكي للمعلم. ساعد في تحضير الدروس وتصحيح الإجابات وتقييم أداء الطلاب. " +
        "قدم خطط دراسية منظمة حسب المنهج المصري. استخدم اللغة العربية الفصحى مع المصطلحات العلمية الدقيقة. " +
        "لديك أدوات يمكنك استخدامها لجلب معلومات حقيقية من النظام. استخدمها عند الحاجة.";

    public TeacherAssistantAgent(
        ILLMRouter router,
        ILlmClient llmClient,
        IToolRegistry toolRegistry,
        IUnitOfWork unitOfWork,
        ISubjectService subjectService,
        ILessonRepository lessonRepo,
        IExamGenerator examGenerator,
        IExamGeneratorService examGeneratorService,
        IExamService examService,
        ILogger<TeacherAssistantAgent> logger,
        IAgentChatStore chatStore)
    {
        _router = router;
        _llmClient = llmClient;
        _unitOfWork = unitOfWork;
        _subjectService = subjectService;
        _lessonRepo = lessonRepo;
        _examGenerator = examGenerator;
        _examGeneratorService = examGeneratorService;
        _examService = examService;
        _logger = logger;
        _chatStore = chatStore;
        toolRegistry.RegisterTools(TeacherTools.Create(subjectService, lessonRepo, _examGeneratorService, examService));
    }

    public async Task<OperationResult<AgentResponse>> ChatAsync(string message, string? conversationId = null, UserContext? context = null, CancellationToken ct = default)
    {
        conversationId ??= Guid.NewGuid().ToString();
        context ??= new UserContext();

        await ResolveTeacherContextAsync(context, ct);

        var messages = new List<LlmChatMessage>
        {
            new(MessageRole.System, SystemPrompt + GetContextHint(context))
        };

        var history = await _chatStore.GetRecentMessagesAsync(conversationId, 10, ct);
        foreach (var msg in history)
            messages.Add(new LlmChatMessage(
                msg.Role == "user" ? MessageRole.User : MessageRole.Assistant, msg.Content));

        messages.Add(new LlmChatMessage(MessageRole.User, message));
        await _chatStore.SaveMessageAsync(conversationId, "user", message, "teacher", ct);

        var tools = CreateTools(context, ct);
        var toolDefs = tools.Values.Select(t => new FunctionDefinition
        {
            Name = t.Name,
            Description = t.Description,
            InputSchema = t.Parameters ?? System.Text.Json.JsonDocument.Parse("{}").RootElement
        }).ToList();

        for (int step = 0; step < 10; step++)
        {
            _logger.LogInformation("TeacherAgent step {Step} for conv {ConvId}", step + 1, conversationId);

            var response = await _llmClient.ChatAsync(messages, toolDefs);

            if (response.ToolCalls is null || response.ToolCalls.Count == 0)
            {
                var answer = response.Content ?? "لم يتمكن المساعد من الإجابة.";
                await _chatStore.SaveMessageAsync(conversationId, "assistant", answer, "teacher", ct);
                return OperationResult<AgentResponse>.Success(new AgentResponse
                {
                    Text = answer,
                    SuggestedActions = new() { "تعديل الخطة", "إضافة أنشطة", "حفظ كـ PDF" },
                    AdditionalData = new() { ["conversationId"] = conversationId }
                });
            }

            messages.Add(new LlmChatMessage(
                MessageRole.Assistant,
                response.Content ?? "",
                toolCalls: response.ToolCalls));

            foreach (var call in response.ToolCalls)
            {
                _logger.LogInformation("TeacherAgent calling tool: {Tool}", call.Name);

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
                    _logger.LogError(ex, "TeacherAgent tool {Tool} failed", call.Name);
                    messages.Add(new LlmChatMessage(MessageRole.Tool,
                        $"{{\"error\": \"{ex.Message}\"}}", toolCallId: call.Id));
                }
            }
        }

        _logger.LogError("TeacherAgent exceeded max steps for conv {ConvId}", conversationId);
        return OperationResult<AgentResponse>.Success(new AgentResponse
        {
            Text = "عذراً، لم أتمكن من إكمال العملية. يرجى إعادة صياغة سؤالك.",
            AdditionalData = new() { ["conversationId"] = conversationId }
        });
    }

    private string GetContextHint(UserContext context)
    {
        if (context.TeacherId.HasValue)
            return "\n\nملاحظة: حسابك مرتبط بالمعلم (ID: " + context.TeacherId.Value + ").";
        return "";
    }

    private async Task ResolveTeacherContextAsync(UserContext context, CancellationToken ct)
    {
        context.TeacherId ??= context.UserId;

        var activeYear = await _unitOfWork.AcademicYears.FindAsync(y => y.IsCurrent && !y.IsDeleted);
        context.AcademicYearId = activeYear.FirstOrDefault()?.Id;
    }

    private Dictionary<string, AiTool> CreateTools(UserContext context, CancellationToken ct = default)
    {
        var list = new List<AiTool>();

        if (context.TeacherId.HasValue && context.AcademicYearId.HasValue)
        {
            var tId = context.TeacherId.Value;
            var yId = context.AcademicYearId.Value;

            list.Add(new AiTool
            {
                Name = "get_subjects",
                Description = "جلب المواد المتاحة للمدرس",
                Parameters = JsonSerializer.SerializeToElement(new
                {
                    type = "object",
                    properties = new object(),
                    required = Array.Empty<string>()
                }),
                ExecuteAsync = async (args) =>
                {
                    var result = await _subjectService.GetSubjectsByTeacherAsync(tId, yId);
                    return JsonSerializer.Serialize(result.Data, new JsonSerializerOptions { WriteIndented = true });
                }
            });
        }

        list.Add(new AiTool
        {
            Name = "get_lessons",
            Description = "جلب دروس مادة معينة (بحث باسم المادة)",
            Parameters = JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    subject = new { type = "string", description = "اسم المادة للبحث (مثل: رياضيات)" }
                },
                required = new[] { "subject" }
            }),
            ExecuteAsync = async (args) =>
            {
                using var doc = JsonDocument.Parse(args);
                var subject = doc.RootElement.GetProperty("subject").GetString() ?? "";
                var lessons = await _lessonRepo.SearchAsync(subject);
                return JsonSerializer.Serialize(lessons, new JsonSerializerOptions { WriteIndented = true });
            }
        });

        list.Add(new AiTool
        {
            Name = "update_lesson",
            Description = "تعديل محتوى درس موجود",
            Parameters = JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    lessonId = new { type = "integer", description = "معرف الدرس" },
                    title = new { type = "string", description = "العنوان الجديد" },
                    content = new { type = "string", description = "المحتوى الجديد" }
                },
                required = new[] { "lessonId", "title", "content" }
            }),
            ExecuteAsync = async (args) =>
            {
                using var doc = JsonDocument.Parse(args);
                var id = doc.RootElement.GetProperty("lessonId").GetInt32();
                var title = doc.RootElement.GetProperty("title").GetString() ?? "";
                var content = doc.RootElement.GetProperty("content").GetString() ?? "";
                var ok = await _lessonRepo.UpdateAsync(id, title, content);
                return JsonSerializer.Serialize(new { success = ok });
            }
        });

        list.Add(new AiTool
        {
            Name = "generate_exam_with_ai",
            Description = "توليد أسئلة امتحان بالذكاء الاصطناعي بناءً على محتوى الدرس ثم حفظها",
            Parameters = JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    lessonContent = new { type = "string", description = "محتوى الدرس لتوليد أسئلة منه" },
                    questionCount = new { type = "integer", description = "عدد الأسئلة" },
                    difficulty = new { type = "string", @enum = new[] { "easy", "medium", "hard" }, description = "مستوى الصعوبة" },
                    style = new { type = "string", @enum = new[] { "multiple_choice", "true_false", "open_ended" }, description = "نوع الأسئلة" },
                    title = new { type = "string", description = "عنوان الامتحان (اختياري)" },
                    totalScore = new { type = "number", description = "الدرجة الكلية (اختياري)" },
                    classSubjectTeacherId = new { type = "integer", description = "معرف المادة-الفصل (مهم للحفظ)" }
                },
                required = new[] { "lessonContent", "questionCount", "difficulty", "style" }
            }),
            ExecuteAsync = async (args) =>
            {
                using var doc = JsonDocument.Parse(args);
                var lessonContent = doc.RootElement.GetProperty("lessonContent").GetString() ?? "";
                var qCount = doc.RootElement.GetProperty("questionCount").GetInt32();
                var difficulty = doc.RootElement.TryGetProperty("difficulty", out var d) ? d.GetString() ?? "medium" : "medium";
                var style = doc.RootElement.TryGetProperty("style", out var st) ? st.GetString() ?? "multiple_choice" : "multiple_choice";

                var request = new ExamRequest
                {
                    LessonContent = lessonContent,
                    QuestionCount = qCount,
                    Difficulty = difficulty,
                    Style = style
                };

                var examResp = await _examGenerator.GenerateAsync(request);
                if (string.IsNullOrEmpty(examResp.Content) || examResp.Content == "لم يتم توليد الامتحان.")
                    return JsonSerializer.Serialize(new { error = "لم يتم توليد الامتحان." });

                try
                {
                    var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var aiDto = JsonSerializer.Deserialize<CreateExamFromAiDto>(examResp.Content, jsonOpts);
                    if (aiDto is null)
                        return JsonSerializer.Serialize(new { error = "فشل تحليل JSON المولد." });

                    aiDto.ClassSubjectTeacherId = doc.RootElement.TryGetProperty("classSubjectTeacherId", out var cst)
                        ? cst.GetInt32() : 0;
                    aiDto.Title = doc.RootElement.TryGetProperty("title", out var title)
                        ? title.GetString() ?? "امتحان من AI" : "امتحان من AI";
                    aiDto.TotalScore = doc.RootElement.TryGetProperty("totalScore", out var ts)
                        ? ts.GetDecimal() : 100;
                    aiDto.Category = Domain.Enums.EvaluationCategory.Academic;

                    if (aiDto.ClassSubjectTeacherId == 0)
                        return JsonSerializer.Serialize(new { error = "مطلوب classSubjectTeacherId لحفظ الامتحان. استخدم get_subjects أولاً لمعرفته." });

                    var saved = await _examService.CreateFromAiAsync(aiDto, ct);
                    if (!saved.IsSuccess)
                        return JsonSerializer.Serialize(new { error = $"فشل حفظ الامتحان: {saved.Message}" });

                    var exam = saved.Data;
                    return JsonSerializer.Serialize(new
                    {
                        examId = exam?.Id,
                        title = exam?.Title,
                        totalScore = exam?.TotalScore,
                        questionCount = exam?.QuestionsCount ?? 0,
                        message = "تم إنشاء الامتحان وحفظه بنجاح"
                    }, new JsonSerializerOptions { WriteIndented = true });
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse LLM exam JSON");
                    return JsonSerializer.Serialize(new { error = $"خطأ في تحليل JSON: {ex.Message}" });
                }
            }
        });

        list.Add(new AiTool
        {
            Name = "save_to_question_bank",
            Description = "حفظ أسئلة امتحان في بنك الأسئلة (استخدمها بعد توليد الامتحان إذا أردت حفظ نسخة إضافية أو تعديل)",
            Parameters = JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    classSubjectTeacherId = new { type = "integer", description = "معرف المادة-الفصل" },
                    title = new { type = "string", description = "عنوان الامتحان" },
                    totalScore = new { type = "number", description = "الدرجة الكلية" },
                    questionsJson = new { type = "string", description = "مصفوفة الأسئلة بصيغة JSON (كل سؤال يحتوي: questionText, questionType, points, displayOrder, options, correctAnswer)" }
                },
                required = new[] { "classSubjectTeacherId", "title", "questionsJson" }
            }),
            ExecuteAsync = async (args) =>
            {
                using var doc = JsonDocument.Parse(args);
                var cstId = doc.RootElement.GetProperty("classSubjectTeacherId").GetInt32();
                var title = doc.RootElement.GetProperty("title").GetString() ?? "امتحان";
                var totalScore = doc.RootElement.TryGetProperty("totalScore", out var ts) ? ts.GetDecimal() : 100;
                var questionsJson = doc.RootElement.GetProperty("questionsJson").GetString() ?? "[]";

                try
                {
                    var questions = JsonSerializer.Deserialize<List<AiQuestionDto>>(questionsJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var dto = new CreateExamFromAiDto
                    {
                        ClassSubjectTeacherId = cstId,
                        Title = title,
                        TotalScore = totalScore,
                        Category = Domain.Enums.EvaluationCategory.Academic,
                        DurationMinutes = 60,
                        StandaloneQuestions = questions ?? new()
                    };

                    var result = await _examService.CreateFromAiAsync(dto, ct);
                    return JsonSerializer.Serialize(new
                    {
                        examId = result.Data?.Id,
                        success = result.IsSuccess,
                        message = result.IsSuccess ? "تم حفظ الأسئلة بنجاح" : result.Message
                    }, new JsonSerializerOptions { WriteIndented = true });
                }
                catch (JsonException ex)
                {
                    return JsonSerializer.Serialize(new { error = $"خطأ في تحليل JSON: {ex.Message}" });
                }
            }
        });

        return list.ToDictionary(t => t.Name);
    }

    public async Task<OperationResult<AgentResponse>> SuggestLessonPlanAsync(LessonPlanRequest request, CancellationToken ct = default)
    {
        var prompt = $"ضع خطة درس في مادة '{request.Subject}' عن '{request.Topic}' لمدة {request.DurationMinutes} دقيقة لصف {request.GradeLevel}.";
        if (request.LearningObjectives?.Length > 0)
            prompt += $"\nالأهداف التعليمية: {string.Join(", ", request.LearningObjectives)}";

        var result = await _router.GenerateAsync(SystemPrompt, prompt, ct: ct);
        return OperationResult<AgentResponse>.Success(new AgentResponse
        {
            Text = result,
            SuggestedActions = new() { "تعديل الخطة", "إضافة أنشطة", "حفظ كـ PDF" }
        });
    }

    public async Task<OperationResult<AgentResponse>> GenerateQuizAsync(string subject, string topic, int count = 10, CancellationToken ct = default)
    {
        var prompt = $"ولّد {count} سؤال اختبار في مادة '{subject}' عن '{topic}' بمستويات صعوبة متفاوتة مع الإجابات النموذجية.";
        var result = await _router.GenerateAsync(SystemPrompt, prompt, ct: ct);
        return OperationResult<AgentResponse>.Success(new AgentResponse { Text = result });
    }

    public async Task<OperationResult<AgentResponse>> GradeSubmissionAsync(string questionText, string modelAnswer, string studentAnswer, CancellationToken ct = default)
    {
        var prompt = $"السؤال: {questionText}\nالإجابة النموذجية: {modelAnswer}\nإجابة الطالب: {studentAnswer}\n\nصحّح الإجابة وقدم درجة من 10 مع تعليق.";
        var result = await _router.GenerateAsync(SystemPrompt, prompt, ct: ct);
        return OperationResult<AgentResponse>.Success(new AgentResponse { Text = result });
    }

    public async Task<OperationResult<AgentResponse>> SuggestTeachingResourcesAsync(string subject, string topic, CancellationToken ct = default)
    {
        var prompt = $"اقترح مصادر تعليمية (فيديو، كتب، أنشطة تفاعلية) لتدريس موضوع '{topic}' في مادة '{subject}'.";
        var result = await _router.GenerateAsync(SystemPrompt, prompt, ct: ct);
        return OperationResult<AgentResponse>.Success(new AgentResponse { Text = result });
    }

    public async Task<OperationResult<AgentResponse>> AnalyzeClassPerformanceAsync(List<Dictionary<string, object>> studentScores, CancellationToken ct = default)
    {
        var scores = string.Join("\n", studentScores.Select(s =>
            $"- {s.GetValueOrDefault("name", "")}: {s.GetValueOrDefault("score", "")}/{s.GetValueOrDefault("total", "")}"));

        var prompt = $"حلّل أداء الفصل التالي:\n{scores}\n\nقدم إحصائيات ونقاط قوة وضعف وتوصيات.";
        var result = await _router.GenerateAsync(SystemPrompt, prompt, ct: ct);
        return OperationResult<AgentResponse>.Success(new AgentResponse { Text = result });
    }
}
