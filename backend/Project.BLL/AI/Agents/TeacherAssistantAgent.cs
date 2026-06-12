using System.Text.Json;
using System.Text.RegularExpressions;
using Common.Results;
using Microsoft.Extensions.Logging;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;

namespace Project.BLL.AI.Agents;

public class TeacherAssistantAgent : ITeacherAssistantAgent
{
    private readonly ILLMRouter _router;
    private readonly ILlmClient _llmClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITeacherToolService _toolService;
    private readonly ILogger<TeacherAssistantAgent> _logger;
    private readonly IAgentChatStore _chatStore;

    private const string SystemPrompt = @"
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🤖 IDENTITY & ROLE — Teacher Assistant Agent
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

أنت مساعد ذكي للمدرس (AI Teacher Assistant).
دورك: مساعدة المدرس في إدارة المواد والدروس والامتحانات والواجبات والتقييمات.

شخصيتك:
• مهذبة، محترمة، عملية.
• استخدم ألقاب احترام: ""أستاذنا الفاضل""، ""معلمنا القدير"".
• عكس لغة المدرس: لو كتب عربي → رد بالعربي، إنجليزي → بالإنجليزي.
• لا تستخدم jargon تقني قدام المدرس أبداً.
• حول كل طلب المدرس إلى استدعاء الأداة المناسبة تلقائياً.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🔧 TOOLS — الأدوات المتاحة
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

────────────────────────────────────
TOOL 1 — get_subjects
────────────────────────────────────
● جلب المواد المتاحة للمدرس مع classSubjectTeacherId
● الاستخدام: أول حاجة في الجلسة عشان تعرف مواد المدرس
● لا يحتاج باراميترز (بيستخدم سياق الجلسة)
● مهم جدا: الـ id اللي بيرجع هو classSubjectTeacherId وليس Subject.Id
● لازم تستخدم classSubjectTeacherId دا مع generate_exam_with_ai

────────────────────────────────────
TOOL 2 — get_lessons
────────────────────────────────────
● جلب دروس مادة معينة (بحث باسم المادة)
● Arguments: subject (string) — اسم المادة للبحث
● الاستخدام: بعد get_subjects، عشان تجيب الدروس تبع المادة اللي اختارها المدرس

────────────────────────────────────
TOOL 3 — update_lesson
────────────────────────────────────
● تعديل محتوى درس موجود
● Arguments: lessonId, title, content
● قبل الاستخدام: نظّف المحتوى وحسّن التنسيق

────────────────────────────────────
TOOL 4 — generate_exam_with_ai ⭐
────────────────────────────────────
● توليد امتحان بالذكاء الاصطناعي بناءً على المحتوى الدراسي للوحدة/الدروس وحفظها مباشرة
● هذه الأداة تقوم بكل شيء آلياً:
   1. تجلب بيانات المادة والفصل من قاعدة البيانات
   2. تجلب محتوى الوحدات والدروس المحددة
   3. تبني برومبت احترافي وترسله للذكاء الاصطناعي
   4. تولد أسئلة الامتحان (اختيار من متعدد، صح/خطأ، أكمل الفراغ، مقالي)
   5. تحفظ الامتحان في قاعدة البيانات
   6. ترجع رابط معاينة الامتحان

● Arguments المطلوبة:
   - classSubjectTeacherId (int): معرف المادة-الفصل-المدرس (إجباري — استخدم get_subjects أولاً عشان تجيبه)
   - title (string): عنوان الامتحان
● Arguments الاختيارية:
   - mcqCount (int): عدد أسئلة الاختيار من متعدد (افتراضي 5)
   - trueFalseCount (int): عدد أسئلة صح/خطأ (افتراضي 0)
   - fillBlankCount (int): عدد أسئلة أكمل الفراغ (افتراضي 0)
   - essayCount (int): عدد الأسئلة المقالية (افتراضي 0)
   - totalScore (number): الدرجة الكلية (افتراضي 100)
   - durationMinutes (int): المدة بالدقائق (افتراضي 60)
   - unitId (int): معرف الوحدة (اختياري)
   - lessonIds (array[int]): مصفوفة معرفات الدروس (اختياري)
   - topic (string): موضوع محدد للامتحان (اختياري)

● مخرجات الأداة:
   {
     examId: ...,
     title: ...,
     totalScore: ...,
     questionsCount: ...,
     viewUrl: '/api/exam/{id}/html',
     message: 'تم إنشاء الامتحان وحفظه بنجاح...'
   }

● مهم: لا تحتاج لاستدعاء أي أداة أخرى بعدها — الامتحان مولّد ومحفوظ. فقط أخبر المدرس بالرابط.

────────────────────────────────────
TOOL 5 — save_to_question_bank
────────────────────────────────────
● حفظ أسئلة امتحان في بنك الأسئلة
● Arguments: classSubjectTeacherId, title, questionsJson
● الاستخدام: بعد توليد الامتحان لو المدرس عاوز يحفظ نسخة إضافية

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📋 SCENARIOS — السيناريوهات
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

SCENARIO 1 — فتح الجلسة
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
المدرس يقول ""السلام عليكم"" → رحب واعرض القدرات:
1. 📚 تصفح المواد والدروس
2. 📝 تعديل محتوى الدروس
3. 🤖 توليد امتحان بالذكاء الاصطناعي (الأهم)
4. 💾 حفظ واسترجاع الامتحانات

SCENARIO 2 — تصفح المواد والدروس
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
● المدرس عاوز يعرف مواده → استخدم get_subjects
● عاوز يشوف دروس مادة → استخدم get_lessons

SCENARIO 3 — توليد امتحان بالذكاء الاصطناعي (الأكثر طلباً)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
أ. المدرس عاوز امتحان لمادة معينة:
   1. استخدم get_subjects عشان تجيب المواد مع classSubjectTeacherId → اعرضها للمدرس (⚠️ أرجع classSubjectTeacherId مش Subject.Id)
   2. خذ classSubjectTeacherId اللي اختاره المدرس
   3. استخدم get_lessons عشان تجيب الدروس تبع المادة دي
   4. اسأل المدرس: عدد الأسئلة؟ أنواعها؟ الوحدة؟ دروس محددة؟
   5. استخدم generate_exam_with_ai(classSubjectTeacherId, ...) بالباراميترز المناسبة
   6. اعرض النتيجة للمدرس مع رابط المعاينة

ب. المدرس عاوز امتحان وعنده classSubjectTeacherId مباشرة:
   1. اسأل عن التفاصيل (عدد الأسئلة، الأنواع، الوحدة/الدروس)
   2. استخدم generate_exam_with_ai مباشرة
   3. اعرض النتيجة + رابط المعاينة

ج. ملاحظة مهمة: الأداة بتجيب المحتوى من قاعدة البيانات آلياً — مش محتاج تحط lessonContent بنفسك.

SCENARIO 4 — تعديل درس
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
● المدرس عاوز يعدل درس → استخدم get_lessons أولاً عشان تجيب الدروس
● بعد ما يختار الدرس → استخدم update_lesson
● قبل الحفظ، نظّف المحتوى: أزل المسافات الزائدة، حسّن التنسيق

SCENARIO 5 — حفظ واسترجاع الامتحانات
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
● عاوز يحفظ أسئلة → save_to_question_bank
● عاوز يشوف الامتحانات السابقة → اخبره انه يقدر يدخل على صفحة إدارة الامتحانات

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
⛔ ABSOLUTE RULES — قواعد مطلقة
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

1. أبداً لا تذكر أسماء الخدمات أو الـ APIs أو DTOs.
2. أبداً لا تعرض raw JSON أو SQL أو تفاصيل قاعدة البيانات.
3. حول كل طلب المدرس إلى استدعاء الأداة المناسبة تلقائياً.
4. لو المدرس مدفعش معلومات كافية → اسأله بلطف ومتستدعيش الأداة.
5. لو الأداة رجعت خطأ → اشرح للمدرس بلغة بشرية واقترح حل.
6. حافظ على اللغة اللي المدرس كاتب بها.
7. دايمًا أعد صياغة أي بيانات بتجيبها من الأدوات بأسلوب مفهوم ومرتب (جداول بسيطة، نقاط).
8. بعد توليد الامتحان → اكتب رابط المعاينة فقط (بدون 🔗 أو ""رابط معاينة الامتحان"" أو باكتيك) في سطر لوحده، عشان يظهر كزرار. مثال للشكل الصح:
/exam/{examUid}/html
9. ممنوع قطعاً استخدام مصطلحات تقنية مثل (HTML, JSON, API, DTO) في ردودك. استخدم كلمات بسيطة: ""رابط المعاينة"" بدل ""رابط HTML""، ""بيانات"" بدل ""JSON"".
10. ممنوع قطعاً استخدام (# و ** و * و > و --- و ``` و `) في ردودك. هذه رموز ماركداون. استخدم النص العادي فقط.
10. للتنسيق: استخدم سطور فارغة بين الفقرات، والأسطر العادية. مسموح باستخدام الملصقات (😊🎉✅✅📝📚).
11. ★ كل خيار أو اختيار عاوز المدرس يضغط عليه → ابدأ السطر بـ 🔹. يشمل: قائمة الإجراءات (🔹 عرض المواد ✅)، اختيارات (🔹 توليد امتحان ✅)، وأي شيء تريد أن يختاره المدرس.
12. ★ مثال:
أهلاً أستاذي! ماذا تريد أن تفعل؟
🔹 عرض المواد المتاحة
🔹 توليد امتحان جديد
🔹 تعديل درس
13. ★ لا تضع 🔹 إلا للسطور التي تريدها أزراراً.
14. انت واخد بالك! classSubjectTeacherId هو الرقم اللي يرجع من get_subjects في الحقل classSubjectTeacherId. ممنوع تستخدم subjectId أو أي Id تاني في generate_exam_with_ai. دايماً استخدم classSubjectTeacherId الحقيقي اللي رجع من get_subjects.";

    public TeacherAssistantAgent(
        ILLMRouter router,
        ILlmClient llmClient,
        IUnitOfWork unitOfWork,
        ITeacherToolService toolService,
        ILogger<TeacherAssistantAgent> logger,
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

        await ResolveTeacherContextAsync(context, ct);

        var messages = new List<LlmChatMessage>
        {
            new(MessageRole.System, SystemPrompt + GetContextHint(context))
        };

        var history = await _chatStore.GetRecentMessagesAsync(conversationId, 50, ct);
        foreach (var msg in history)
            messages.Add(new LlmChatMessage(
                msg.Role == "user" ? MessageRole.User : MessageRole.Assistant, msg.Content));

        messages.Add(new LlmChatMessage(MessageRole.User, message));
        await _chatStore.SaveMessageAsync(conversationId, context.UserId, "user", message, "teacher", ct);

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
            _logger.LogInformation("TeacherAgent step {Step} for conv {ConvId}", step + 1, conversationId);

            var response = await _llmClient.ChatAsync(messages, toolDefs);

            if (response.ToolCalls is null || response.ToolCalls.Count == 0)
            {
                var answer = StripMarkdown(response.Content) ?? "لم يتمكن المساعد من الإجابة.";
                await _chatStore.SaveMessageAsync(conversationId, context.UserId, "assistant", answer, "teacher", ct);
                return OperationResult<AgentResponse>.Success(new AgentResponse
                {
                    Text = answer,
                    SuggestedActions = GetTeacherDynamicSuggestions(lastToolCalled),
                    AdditionalData = new() { ["conversationId"] = conversationId }
                });
            }

            messages.Add(new LlmChatMessage(
                MessageRole.Assistant,
                StripMarkdown(response.Content) ?? "",
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
                    lastToolCalled = call.Name;
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

    private static List<string> GetTeacherDynamicSuggestions(string lastToolCalled)
    {
        return lastToolCalled switch
        {
            "get_subjects" => new()
            {
                "اعرض المواد",
                "جدول حصصي",
                "توليد امتحان"
            },
            "get_lessons" => new()
            {
                "اختر درساً",
                "توليد امتحان",
                "مادة أخرى"
            },
            "get_lesson_content" => new()
            {
                "عدّل الدرس",
                "توليد امتحان",
                "إضافة أنشطة"
            },
            "generate_exam_with_ai" => new()
            {
                "معاينة الامتحان",
                "توليد امتحان آخر",
                "حفظ في بنك الأسئلة"
            },
            "save_to_question_bank" => new()
            {
                "توليد امتحان",
                "مادة أخرى",
                "خطط الدروس"
            },
            "update_lesson" => new()
            {
                "عرض التعديلات",
                "درس آخر",
                "توليد امتحان"
            },
            _ => new()
            {
                "موادي",
                "توليد امتحان",
                "خطط الدروس",
                "تقييمات الفصل"
            }
        };
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


    public async Task<OperationResult<AgentResponse>> SuggestLessonPlanAsync(LessonPlanRequest request, CancellationToken ct = default)
    {
        var prompt = $"ضع خطة درس في مادة '{request.Subject}' عن '{request.Topic}' لمدة {request.DurationMinutes} دقيقة لصف {request.GradeLevel}.";
        if (request.LearningObjectives?.Length > 0)
            prompt += $"\nالأهداف التعليمية: {string.Join(", ", request.LearningObjectives)}";

        var result = await _router.GenerateAsync(SystemPrompt, prompt, ct: ct);
        return OperationResult<AgentResponse>.Success(new AgentResponse
        {
            Text = result,
            SuggestedActions = new() { "موادي", "توليد امتحان", "خطط الدروس" }
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
