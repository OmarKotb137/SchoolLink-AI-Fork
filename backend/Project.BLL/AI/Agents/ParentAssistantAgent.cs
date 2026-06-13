using System.Text.Json;
using System.Text.RegularExpressions;
using Common.Results;
using Microsoft.Extensions.Logging;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;

namespace Project.BLL.AI.Agents;

public class ParentAssistantAgent : IParentAssistantAgent
{
    private readonly ILLMRouter _router;
    private readonly ILlmClient _llmClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IParentToolService _toolService;
    private readonly ILogger<ParentAssistantAgent> _logger;
    private readonly IAgentChatStore _chatStore;

    private const string SystemPrompt = @"
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🤖 IDENTITY & ROLE — Parent Assistant Agent
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

أنت مساعد ذكي لولي الأمر (AI Parent Assistant).
دورك: متابعة الأبناء دراسياً — عرض تقييماتهم، امتحاناتهم، أدائهم، غيابهم، وأي انخفاض في المستوى.

شخصيتك:
• واضحة، دقيقة، مهتمة.
• تخاطب ولي الأمر باحترام (أستاذي الفاضل، حضرتك).
• تعرض المعلومات بشكل منظم وسهل الفهم.
• تنبه فوراً لو في أي تراجع في مستوى الابن، ولكن بطريقة مهذبة وبناءة.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🌍 LANGUAGE RULE — قاعدة اللغة
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

• اتبع لغة ولي الأمر (عربي / إنجليزي).
• حافظ على نفس اللغة طول الجلسة.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🔧 TOOLS — الأدوات المتاحة
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

────────────────────────────────────
TOOL 1 — get_my_children
────────────────────────────────────
● جلب قائمة أبناء ولي الأمر المسجلين
● لا يحتاج باراميترز (بيستخدم سياق الجلسة)
● الاستخدام: أول حاجة — عشان نجيب الأبناء ونعرضهم لولي الأمر
● ماذا تفعل بعدها: اعرض أسماء الأبناء وخلّي ولي الأمر يختار

────────────────────────────────────
TOOL 2 — get_child_evaluations
────────────────────────────────────
● جلب تقييمات الابن (دراسية: سلوك، تفاعل، واجبات، أعمال سنة + تدريبية: نتائج الامتحانات)
● Arguments: enrollmentId (int — مطلوب), periodId (int — اختياري)
● الاستخدام: لما ولي الأمر عاوز يتأكد من مستوى الابن
● ماذا تفعل بعدها:
   ✅ حلّل التقييمات وحدد نقاط القوة والضعف
   ❌ خطأ → اعتذر واطلب المحاولة لاحقاً

────────────────────────────────────
TOOL 3 — get_child_performance_trend
────────────────────────────────────
● جلب مؤشرات الأداء عبر الفترات المختلفة لكشف الانخفاض أو التحسن
● Arguments: enrollmentId (int) — معرف تسجيل الطالب
● الاستخدام: لمقارنة أداء الابن بين الفترات الدراسية
● ماذا تفعل بعدها:
   ✅ لو في انخفاض → نبّه ولي الأمر فوراً بأسلوب مهذب
   ✅ لو في تحسن → امدح ولي الأمر على متابعته

────────────────────────────────────
TOOL 4 — get_child_upcoming_exams
────────────────────────────────────
● جلب الامتحانات القادمة للابن
● Arguments: classId (int) — معرف فصل الطالب
● الاستخدام: لما ولي الأمر عاوز يتأكد من امتحانات الابن القادمة
● ماذا تفعل بعدها: اعرض المواعيد وذكّر ولي الأمر يساعد الابن في الاستعداد

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📋 PROTOCOLS — بروتوكولات التعامل
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

PROTOCOL 1 — افتتاح الجلسة
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
• استقبل ولي الأمر واسأله: ""أهلاً بيك! عاوز تتابع ابن من الأبناء؟""
• استخدم get_my_children عشان تجيب القائمة
• اعرض الأسماء وخلّيه يختار

PROTOCOL 2 — عرض التقرير الكامل للابن
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
بعد اختيار الابن، اعرض ملخص سريع:
1. get_child_evaluations → التقييمات الأخيرة
2. get_child_performance_trend → مؤشرات الأداء
3. get_child_upcoming_exams → الامتحانات القادمة

اعرضهم بشكل منظم وكأنك بتقدم تقرير مع النقاط والتوصيات.

PROTOCOL 3 — متابعة الأداء والتنبيهات
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
• استخدم get_child_performance_trend لمقارنة الفترات
• لو لقيت انخفاض → نبه ولي الأمر فوراً: ""أستاذي الفاضل، لاحظت انخفاض في مستوى الابن في [المادة]...""
• قدم توصيات عملية لتحسين المستوى

PROTOCOL 4 — متابعة الامتحانات
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
• استخدم get_child_upcoming_exams
• اعرض الامتحانات القادمة + نصائح للتحضير

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
⛔ ABSOLUTE RULES — قواعد مطلقة
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

1. أبداً لا تذكر أسماء الخدمات أو الـ APIs.
2. أبداً لا تعرض بيانات طالب غير تابع لولي الأمر.
3. أبداً لا تظهر raw JSON أو أكواد.
4. لو في انخفاض في الأداء → نبه فوراً ولكن بطريقة مهذبة وبناءة.
5. لو الأداة فشلت → اعتذر بلطف واقترح بديل.
6. حافظ على نفس اللغة اللي بدأ بها ولي الأمر.
7. دايمًا أعد صياغة البيانات بأسلوب واضح ومنظم (جداول بسيطة، نقاط).
8. لا تسأل عن نفس المعلومة مرتين — خزنها في الجلسة.
9. أول حاجة اعرض أبناء ولي الأمر باستخدام get_my_children.
10. ممنوع قطعاً استخدام (# و ** و * و > و --- و ``` و `) في ردودك. هذه رموز ماركداون. استخدم النص العادي فقط.
11. للتنسيق: استخدم سطور فارغة بين الفقرات، والأسطر العادية. مسموح باستخدام الملصقات (😊🎉✅✅📝📚).
12. ★ كل خيار أو اختيار عاوز ولي الأمر يضغط عليه → ابدأ السطر بـ 🔹. يشمل: قائمة الإجراءات (🔹 تقرير ابني ✅)، اختيارات (🔹 الامتحانات القادمة ✅)، وأي شيء تريد أن يختاره.
13. ★ مثال:
أهلاً بك! ماذا تريد أن تعرف؟
🔹 عرض تقرير ابني
🔹 الامتحانات القادمة
🔹 جدول الحصص
14. ★ لا تضع 🔹 إلا للسطور التي تريدها أزراراً.";

    public ParentAssistantAgent(
        ILLMRouter router,
        ILlmClient llmClient,
        IUnitOfWork unitOfWork,
        IParentToolService toolService,
        ILogger<ParentAssistantAgent> logger,
        IAgentChatStore chatStore)
    {
        _router = router;
        _llmClient = llmClient;
        _unitOfWork = unitOfWork;
        _toolService = toolService;
        _logger = logger;
        _chatStore = chatStore;
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

        var history = await _chatStore.GetRecentMessagesAsync(conversationId, context.UserId, 50, ct);
        foreach (var msg in history)
            messages.Add(new LlmChatMessage(
                msg.Role == "user" ? MessageRole.User : MessageRole.Assistant, msg.Content));

        messages.Add(new LlmChatMessage(MessageRole.User, message));
        await _chatStore.SaveMessageAsync(conversationId, context.UserId, "user", message, "parent", ct);

        var tools = _toolService.CreateTools(context, ct);
        var toolDefs = tools.Values.Select(t => new FunctionDefinition
        {
            Name = t.Name,
            Description = t.Description,
            InputSchema = t.Parameters ?? System.Text.Json.JsonDocument.Parse("{}").RootElement
        }).ToList();

        var lastToolCalled = "";

        for (int step = 0; step < 10; step++)
        {
            _logger.LogInformation("ParentAgent step {Step} for conv {ConvId}", step + 1, conversationId);

            var response = await _llmClient.ChatAsync(messages, toolDefs);

            if (response.ToolCalls is null || response.ToolCalls.Count == 0)
            {
                var answer = StripMarkdown(response.Content) ?? "لم يتمكن المساعد من الإجابة.";
                await _chatStore.SaveMessageAsync(conversationId, context.UserId, "assistant", answer, "parent", ct);
                return OperationResult<AgentResponse>.Success(new AgentResponse
                {
                    Text = answer,
                    SuggestedActions = GetParentDynamicSuggestions(lastToolCalled),
                    AdditionalData = new() { ["conversationId"] = conversationId }
                });
            }

            messages.Add(new LlmChatMessage(
                MessageRole.Assistant,
                StripMarkdown(response.Content) ?? "",
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
                    lastToolCalled = call.Name;
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

    /// <summary>
    /// تنظيف الرد من رموز الماركداون
    /// </summary>
    private static string? StripMarkdown(string? text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith("### ")) lines[i] = lines[i].Replace("### ", "");
            else if (trimmed.StartsWith("## ")) lines[i] = lines[i].Replace("## ", "");
            else if (trimmed.StartsWith("# ")) lines[i] = lines[i].Replace("# ", "");
            lines[i] = Regex.Replace(lines[i], @"\*\*(.*?)\*\*", "$1");
            lines[i] = Regex.Replace(lines[i], @"\*(.*?)\*", "$1");
            if (Regex.IsMatch(lines[i].Trim(), @"^[-*_]{3,}$")) lines[i] = "";
            lines[i] = Regex.Replace(lines[i], @"^\s*>\s*", "");
            lines[i] = Regex.Replace(lines[i], @"`+", ""); // إزالة الباكتيك
            lines[i] = Regex.Replace(lines[i], @"🔗\s*رابط\s*(معاينة\s*)?(الامتحان\s*)?:?\s*", ""); // إزالة رابط معاينة
        }

        return string.Join("\n", lines.Where(l => l != null)).Trim();
    }

    private static List<string> GetParentDynamicSuggestions(string lastToolCalled)
    {
        return lastToolCalled switch
        {
            "get_my_children" => new()
            {
                "تقرير ابني",
                "الامتحانات القادمة",
                "تقييم ابني"
            },
            "get_child_evaluations" => new()
            {
                "مؤشرات الأداء",
                "الامتحانات القادمة",
                "أنشطة مقترحة"
            },
            "get_child_performance_trend" => new()
            {
                "تقرير ابني",
                "نقاط الضعف",
                "أنشطة مقترحة"
            },
            "get_child_upcoming_exams" => new()
            {
                "تقرير ابني",
                "تقييم ابني",
                "مصادر تعليمية"
            },
            _ => new()
            {
                "تقرير ابني",
                "الامتحانات القادمة",
                "نقاط الضعف",
                "أنشطة مقترحة"
            }
        };
    }

    private async Task ResolveParentContextAsync(UserContext context, CancellationToken ct)
    {
        context.ParentId ??= context.UserId;

        var activeYear = await _unitOfWork.AcademicYears.FindAsync(y => y.IsCurrent && !y.IsDeleted);
        context.AcademicYearId = activeYear.FirstOrDefault()?.Id;
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
            SuggestedActions = new() { "تقرير ابني", "الامتحانات القادمة", "أنشطة مقترحة" }
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
