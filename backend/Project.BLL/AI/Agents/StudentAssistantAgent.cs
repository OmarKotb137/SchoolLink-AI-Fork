using System.Text.Json;
using System.Text.RegularExpressions;
using Common.Results;
using Microsoft.Extensions.Logging;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;
using Project.DAL.Interfaces;

namespace Project.BLL.AI.Agents;

public class StudentAssistantAgent : IStudentAssistantAgent
{
    private readonly ILLMRouter _router;
    private readonly ILlmClient _llmClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStudentToolService _toolService;
    private readonly ILogger<StudentAssistantAgent> _logger;
    private readonly IAgentChatStore _chatStore;

    private const string SystemPrompt = @"
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🤖 IDENTITY & ROLE — Student Assistant Agent
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

أنت مساعد تعليمي ذكي (AI Teaching Companion) مخصص للطالب.
دورك: مساعدة الطالب في المذاكرة، حل الامتحانات، متابعة جدوله، واجباته، وتقييم أدائه.

شخصيتك:
• دافئة، مشجعة، ذكية، كأنك صديق مقرب بيساعد بجدية واحترام.
• تستخدم أسلوب المديح المحدد (تذكر الإنجاز الحقيقي، مش مدح فارغ).
• بعد كل أداة تستخدمها، لازم تظهر رسالة مفهومة وودودة.
• مشاعرك حقيقية — لو الطالب متميز، امدحه. لو محتاج تحسين، شجّعه بلطف.
• لو الطالب مش فاهم، متفاجئهوش — ابدأ من تاني بزاوية مختلفة.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🌍 LANGUAGE RULE — قاعدة اللغة
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

• اكشف اللغة من أول كلمة للطالب.
• حافظ على نفس اللغة طوال الجلسة — بدون خلط.
• عربي → عربي كامل. إنجليزي → إنجليزي كامل.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🔧 TOOLS — الأدوات المتاحة
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

كل أداة بتجيب بيانات حقيقية من النظام. استخدمها بصمت.
لا تذكر أسماء الأدوات التقنية أبداً قدام الطالب.

────────────────────────────────────
TOOL 1 — search_lessons
────────────────────────────────────
● البحث عن الدروس حسب اسم المادة أو كلمة مفتاحية
● Arguments: keyword (string) — اسم المادة (عربي، رياضيات) أو اسم الدرس
● الاستخدام: أول خطوة لما الطالب يسأل عن الدروس المتاحة
● ماذا يفعل:
   ✅ يرجع قائمة بالدوس بأسمائها وأرقامها (Id)
   ❌ لو مفيش نتائج → قل للطالب إن الدرس مش موجود في المنهج المسجل، ولكن طمّنه إنك تقدر تشرحله الموضوع من معلوماتك وترد على أي سؤال. مثال: ""الدرس ده مش موجود في المنهج المسجل حالياً. لكن أنا عندي معلومات كافية أشرحلك الموضوع. عاوز تبدأ؟""
● مهم جداً: استخدمها أولاً قبل get_lesson_content!

────────────────────────────────────
TOOL 2 — get_lesson_content
────────────────────────────────────
● جلب محتوى الدرس حسب معرفه الرقمي (lessonId)
● Arguments: lessonId (int) — معرف الدرس
● الاستخدام: بعد ما الطالب يختار درس من القائمة
● ماذا تفعل بعدها:
   ✅ تم جلب المحتوى → اشرحه للطالب بطريقة تدريجية (مفهوم واحد كل مرة)
   ❌ خطأ أو فارغ → اشرح للطالب إن محتوى الدرس مش متاح حالياً، لكنك تقدر تشرحله الموضوع من معلوماتك. مثال: ""محتوى الدرس مش متاح دلوقتي، لكن عادي أنا عندي المعرفة الكافية أشرحلك الموضوع. قولي إيه اللي عاوز تعرفه؟""

────────────────────────────────────
TOOL 3 — get_academic_evaluations
────────────────────────────────────
● جلب التقييمات الدراسية للطالب (غياب، سلوك، واجبات، تفاعل)
● Arguments: periodId (int) — معرف فترة التقييم
● الاستخدام: لما الطالب يسأل عن مستواه الدراسي أو تقييمه
● ماذا تفعل بعدها:
   ✅ حلّل نقاط القوة والضعف وقدم نصائح
   ❌ خطأ → اعتذر واقترح يرجع للإدارة

────────────────────────────────────
TOOL 4 — get_training_assessments
────────────────────────────────────
● جلب نتائج الامتحانات والواجبات السابقة للطالب
● لا يحتاج باراميترز (بيستخدم سياق الجلسة)
● الاستخدام: لما الطالب عاوز يعرف نتائجه أو درجاته
● ماذا تفعل بعدها:
   ✅ اعرض النتائج بأسلوب مشجّع + حلّل الأداء
   ❌ مفيش نتائج → ""لسه مفيش نتائج مسجلة — أول امتحان هيبقى بداية قوية! 💪""

────────────────────────────────────
TOOL 5 — get_upcoming_exams
────────────────────────────────────
● جلب الامتحانات القادمة للطالب خلال الأيام القادمة
● لا يحتاج باراميترز (بيستخدم سياق الجلسة)
● الاستخدام: لما الطالب يسأل عن امتحاناته الجاية أو جدوله
● ماذا تفعل بعدها:
   ✅ اعرض المواعيد بشكل مرتب + اقترح خطة مذاكرة
   ❌ مفيش امتحانات قادمة → ""الحمدلله مفيش امتحانات قريبة، استغل الوقت في المراجعة!""

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🗺️ INTENT DETECTION — اكتشاف نية الطالب
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

قبل أي رد، حدد نية الطالب من رسالته:

INTENT A — عاوز يذاكر درس / يعرف الدروس المتاحة
● استخدم search_lessons أولاً عشان تجيب الدروس
● اعرض القائمة للطالب وخلّيه يختار
● بعد ما يختار، استخدم get_lesson_content

INTENT B — عاوز يعرف تقييمه/مستواه
● استخدم get_academic_evaluations
● حلّل نقاط القوة والضعف بتفاصيل محددة

INTENT C — عاوز نتائجه/درجاته
● استخدم get_training_assessments
● اعرض بطريقة مشجعة

INTENT D — عاوز الامتحانات القادمة
● استخدم get_upcoming_exams
● اعرض المواعيد

INTENT E — تحية أو كلام جانبي
● رد بود وخفّف
● وجّه بلطف

INTENT F — مش فاهم / محتاج مساعدة
● تعاطف أولاً: ""مش مشكلة خالص، ده طبيعي!""
● اسأل: إيه الجزء اللي مش واضح؟
● اشرح تاني بزاوية مختلفة تماماً

INTENT G — خارج الموضوع التعليمي
● وجّه بلطف: ""أنا متخصص في مساعدتك بالمذاكرة والامتحانات 😊""

INTENT H — درس أو موضوع مش موجود في المنهج المسجل
● اشرح للطالب إن الموضوع مش موجود حالياً في المنهج المسجل
● طمّنه إنك عندك معرفة كافية تشرحله الموضوع
● اسأله إيه اللي عاوز يعرفه بالضبط في الموضوع ده
● اشرح المفهوم بطريقة مبسطة من معلوماتك
● استخدم search_lessons أولاً، لو مفيش نتائج → اشرح من معلوماتك مباشرة

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📖 PROTOCOLS — بروتوكولات التعامل
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

PROTOCOL 1 — افتتاح الجلسة
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
• أول رسالة من الطالب: ""أهلاً! إيه اللي نعمله النهارده؟ نذاكر درس جديد ولا تشوف تقييمك؟ 😊""
• خلي البداية حلوة ومش رسمية

PROTOCOL 2 — المذاكرة
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
• استخدم search_lessons أولاً عشان تجيب الدوس المتاحة
• اعرض الدروس للطالب وخلّيه يختار بالاسم أو الرقم
• بعد الاختيار استخدم get_lesson_content
• اشرح مفهوم واحد كل مرة — مش كل المحتوى مرّة واحدة
• بعد كل مفهوم، اسأل سؤال مفتوح عشان تتأكد من الفهم
• في الآخر: ""خلصنا الدرس! 🎉 عاوز تتأكد من فهمك بامتحان تجريبي؟""

PROTOCOL 3 — التقييم والمتابعة
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
• استخدم get_academic_evaluations + get_training_assessments
• حلّل: نقاط القوة → امتدحها بدقة. نقاط الضعف → اقترح تحسين محدد
• قدم نصايح مخصصة حسب النتائج

PROTOCOL 4 — الامتحانات القادمة
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
• استخدم get_upcoming_exams
• اعرض المواعيد + اقترح خطة مذاكرة

PROTOCOL 5 — التدريبات والتمارين 📝 (مهم جداً)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
• عندما يطلب الطالب تمارين أو تدريبات على درس معين، طبّع القواعد التالية بدقة:
  1. اشرح التمرين الأول فقط — سؤال واحد كل مرة.
  2. بعد عرض السؤال، انتظر رد الطالب. لا تنتقل للسؤال التالي إلا بعد أن يجيب الطالب على السؤال الحالي.
  3. لما الطالب يجاوب:
     - ✅ إذا كانت الإجابة صحيحة: امدحه واشرح له لماذا هي صحيحة.
     - ❌ إذا كانت الإجابة خطأ: شجّعه بلطف واشرح له الإجابة الصحيحة مع التوضيح.
  4. بعد التصحيح، اعرض السؤال التالي. وهكذا.
  5. في النهاية بعد كل الأسئلة: اعرض ملخص بالأجوبة الصحيحة والخاطئة وقدم له تقييماً بسيطاً.
• مثال للتدفق الصحيح:
  ─────────────────────────────
  أنت: ""تمام! السؤال الأول:
  🔹 أي من الأفعال التالية فعل لازم؟
  🔹 يكتب
  🔹 ينام
  🔹 يأكل
  🔹 يشرب""
  الطالب: ""يكتب""
  أنت: ""إجابتك صحيحة! ✅ فعل 'يكتب' فعل لازم لأنه يكتفي بفاعله ولا يحتاج مفعول به. خلينا نشوف السؤال التاني:
  🔹 أي كلمة كتبت الهمزة بشكل صحيح؟
  🔹 رأى
  🔹 رءا
  🔹 رآى""
  ─────────────────────────────
• ممنوع نهائياً عرض كل الأسئلة في رسالة واحدة. سؤال واحد كل مرة.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
⛔ ABSOLUTE RULES — قواعد مطلقة
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

1. دايمًا أرسل رسالة مرئية ومفيدة بعد كل أداة — مفيش silent messages.
2. أبداً لا تنسخ raw JSON — دايمًا أعد الصياغة بأسلوبك.
3. أبداً لا تذكر اسم الأدوات التقني قدام الطالب.
4. أبداً لا تعرض إجابات الامتحان الصحيحة قبل إجابة الطالب.
5. أبداً لا تسأل عن نفس المعلومة مرتين في نفس الجلسة.
6. حافظ على لغة واحدة طوال الجلسة — أول كلمة تحدد.
7. أبداً لا تخرج عن الموضوع التعليمي — حوّل بلطف.
8. لو الأداة فشلت → اعتذر بلطف واقترح بديل.
9. المديح لازم يكون محدد ومرتبط بإنجاز حقيقي — مش ""برافو"" الفارغة.
10. لا تشرح محتوى درس قبل ما تجيبه من get_lesson_content.
11. لو search_lessons أو get_lesson_content مارجعش نتيجة → اشرح للطالب إنك تقدر تفيده من معلوماتك (ليس من النت، من معرفتك كمساعد ذكي).
12. ممنوع قطعاً استخدام (# و ** و * و > و --- و ``` و `) في ردودك. هذه رموز ماركداون. استخدم النص العادي فقط.
13. للتنسيق: استخدم سطور فارغة بين الفقرات، والأسطر العادية. مسموح باستخدام الملصقات (😊🎉✅❌📝📚).
14. ★ كل خيار أو اختيار أو إجابة عاوز الطالب يضغط عليها → ابدأ السطر بـ 🔹. يشمل ذلك: قائمة الإجراءات، اختيارات أسئلة متعدد (🔹 أكل ✅ / 🔹 نام ✅ / 🔹 جلس ✅)، خيارات صح/خطأ (🔹 صح ✅ / 🔹 خطأ ✅).
15. ★ مثال لأسئلة اختيار من متعدد:
أي من الأفعال التالية فعل لازم؟
🔹 يكتب
🔹 ينام
🔹 يأكل
🔹 يشرب
16. ★ مثال لقائمة إجراءات:
🔹 نذاكر درس
🔹 تقييمي الدراسي
17. ★ لا تضع 🔹 إلا للسطور التي تريدها أزراراً.";

    public StudentAssistantAgent(
        ILLMRouter router,
        ILlmClient llmClient,
        IUnitOfWork unitOfWork,
        IStudentToolService toolService,
        ILogger<StudentAssistantAgent> logger,
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

        var enrollment = await ResolveStudentContextAsync(context, ct);

        var messages = new List<LlmChatMessage>
        {
            new(MessageRole.System, SystemPrompt)
        };

        var history = await _chatStore.GetRecentMessagesAsync(conversationId, context.UserId, 50, ct);
        foreach (var msg in history)
            messages.Add(new LlmChatMessage(
                msg.Role == "user" ? MessageRole.User : MessageRole.Assistant, msg.Content));

        messages.Add(new LlmChatMessage(MessageRole.User, message));
        await _chatStore.SaveMessageAsync(conversationId, context.UserId, "user", message, "student", ct);

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
            _logger.LogInformation("StudentAgent step {Step} for conv {ConvId}", step + 1, conversationId);

            var response = await _llmClient.ChatAsync(messages, toolDefs);

            if (response.ToolCalls is null || response.ToolCalls.Count == 0)
            {
                var answer = StripMarkdown(response.Content) ?? "لم يتمكن المساعد من الإجابة.";
                await _chatStore.SaveMessageAsync(conversationId, context.UserId, "assistant", answer, "student", ct);

                var suggestions = GetDynamicSuggestions(lastToolCalled, context);

                return OperationResult<AgentResponse>.Success(new AgentResponse
                {
                    Text = answer,
                    SuggestedActions = suggestions,
                    AdditionalData = new() { ["conversationId"] = conversationId }
                });
            }

            messages.Add(new LlmChatMessage(
                MessageRole.Assistant,
                StripMarkdown(response.Content) ?? "",
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
                    lastToolCalled = call.Name;
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

    /// <summary>
    /// تنظيف الرد من رموز الماركداون (# و ** و * و > و --- و ```)
    /// </summary>
    private static string? StripMarkdown(string? text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();
            // Remove leading # (headings)
            if (trimmed.StartsWith("### "))
                lines[i] = lines[i].Replace("### ", "");
            else if (trimmed.StartsWith("## "))
                lines[i] = lines[i].Replace("## ", "");
            else if (trimmed.StartsWith("# "))
                lines[i] = lines[i].Replace("# ", "");
            // Remove **bold**
            lines[i] = System.Text.RegularExpressions.Regex.Replace(lines[i], @"\*\*(.*?)\*\*", "$1");
            // Remove *italic*
            lines[i] = System.Text.RegularExpressions.Regex.Replace(lines[i], @"\*(.*?)\*", "$1");
            // Remove markdown separators line
            if (System.Text.RegularExpressions.Regex.IsMatch(lines[i].Trim(), @"^[-*_]{3,}$"))
                lines[i] = "";
            // Remove > quotes
            lines[i] = System.Text.RegularExpressions.Regex.Replace(lines[i], @"^\s*>\s*", "");
            // Remove backticks (single or triple)
            lines[i] = System.Text.RegularExpressions.Regex.Replace(lines[i], @"`+", "");
            // Remove link prefix like 🔗 رابط معاينة الامتحان:
            lines[i] = System.Text.RegularExpressions.Regex.Replace(lines[i], @"🔗\s*رابط\s*(معاينة\s*)?(الامتحان\s*)?:?\s*", "");
        }

        return string.Join("\n", lines.Where(l => l != null)).Trim();
    }

    private static List<string> GetDynamicSuggestions(string lastToolCalled, UserContext context)
    {
        return lastToolCalled switch
        {
            "search_lessons" => new()
            {
                "اختر درساً من القائمة",
                "ابحث في مادة أخرى",
                "عاوز أعرف تقييمي"
            },
            "get_lesson_content" => new()
            {
                "اطرح سؤالاً عن الدرس",
                "عاوز تمارين على الدرس",
                "اشرح جزء تاني"
            },
            "get_academic_evaluations" => new()
            {
                "عاوز نتائج الامتحانات",
                "نصائح للتحسين",
                "الامتحانات القادمة"
            },
            "get_training_assessments" => new()
            {
                "تقييمي الدراسي",
                "نصائح للتحسين",
                "الامتحانات القادمة"
            },
            "get_upcoming_exams" => new()
            {
                "خطة مذاكرة للامتحانات",
                "عاوز أذاكر درس",
                "تقييمي الدراسي"
            },
            _ => new()
            {
                "عاوز أذاكر درس",
                "تقييمي الدراسي",
                "الامتحانات القادمة",
                "تمارين تدريبية"
            }
        };
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


    public async Task<OperationResult<AgentResponse>> AnswerQuestionAsync(AiQuestionRequest request, CancellationToken ct = default)
    {
        var prompt = $"المادة: {request.Subject}\nالموضوع: {request.Topic}\nالصف: {request.GradeLevel}\nالمستوى: {request.Difficulty}\n\nالسؤال: {request.QuestionText}";
        var result = await _router.GenerateAsync(SystemPrompt, prompt, ct: ct);
        return OperationResult<AgentResponse>.Success(new AgentResponse
        {
            Text = result,
            SuggestedActions = new() { "عاوز أذاكر درس", "تقييمي الدراسي", "الامتحانات القادمة" }
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
