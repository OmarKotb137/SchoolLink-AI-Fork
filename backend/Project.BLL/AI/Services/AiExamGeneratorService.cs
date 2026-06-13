using System.Text.Json;
using Common.Results;
using Microsoft.Extensions.Logging;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;
using Project.BLL.DTOs;
using Project.BLL.DTOs.Exam;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Enums;

namespace Project.BLL.AI.Services;

public class AiExamGeneratorService : IAiExamGeneratorService
{
    private readonly ILlmClient _llmClient;
    private readonly IExamService _examService;
    private readonly IClassSubjectTeacherService _cstService;
    private readonly IUnitService _unitService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AiExamGeneratorService> _logger;

    public AiExamGeneratorService(
        ILlmClient llmClient,
        IExamService examService,
        IClassSubjectTeacherService cstService,
        IUnitService unitService,
        IUnitOfWork unitOfWork,
        ILogger<AiExamGeneratorService> logger)
    {
        _llmClient = llmClient;
        _examService = examService;
        _cstService = cstService;
        _unitService = unitService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<OperationResult<GetExamDto>> GenerateExamAsync(
        AiGenerateExamRequest request, CancellationToken ct = default)
    {
        var genResult = await GenerateCoreAsync(request, ct);
        if (!genResult.IsSuccess)
            return OperationResult<GetExamDto>.Failure(genResult.Message ?? "فشل إنشاء الامتحان");

        var (createDto, _) = genResult.Data!;
        return await _examService.CreateFromAiAsync(createDto, ct);
    }

    public async Task<OperationResult<AiExamPreviewDto>> PreviewExamAsync(
        AiGenerateExamRequest request, CancellationToken ct = default)
    {
        var genResult = await GenerateCoreAsync(request, ct);
        if (!genResult.IsSuccess)
            return OperationResult<AiExamPreviewDto>.Failure(genResult.Message ?? "فشل إنشاء الامتحان");

        var (createDto, cst) = genResult.Data;

        var preview = new AiExamPreviewDto
        {
            SubjectName = cst?.SubjectName ?? "",
            ClassName = cst?.ClassName ?? "",
            TeacherName = cst?.TeacherName ?? "",
            Title = createDto.Title,
            DurationMinutes = createDto.DurationMinutes,
            TotalScore = createDto.TotalScore,
            QuestionsCount = createDto.StandaloneQuestions.Count,
            StandaloneQuestions = createDto.StandaloneQuestions.Select(q => new AiExamPreviewQuestionDto
            {
                QuestionText = q.QuestionText,
                QuestionType = (int)q.QuestionType,
                Options = q.Options?.Select(o => new AiExamPreviewOptionDto
                {
                    OptionText = o.Text,
                    IsCorrect = o.IsCorrect,
                    DisplayOrder = o.DisplayOrder
                }).ToList(),
                CorrectAnswer = q.CorrectAnswer,
                Points = q.Points,
                DisplayOrder = q.DisplayOrder
            }).ToList()
        };

        return OperationResult<AiExamPreviewDto>.Success(preview, "تم إنشاء الامتحان بنجاح (لم يتم حفظه بعد)");
    }

    public async Task<OperationResult<GetExamDto>> SaveGeneratedExamAsync(
        CreateExamFromAiDto dto, CancellationToken ct = default)
    {
        return await _examService.CreateFromAiAsync(dto, ct);
    }

    private async Task<OperationResult<(CreateExamFromAiDto CreateDto, ClassSubjectTeacherDto? Cst)>> GenerateCoreAsync(
        AiGenerateExamRequest request, CancellationToken ct = default)
    {
        ClassSubjectTeacherDto? cstData = null;

        if (request.ClassSubjectTeacherId.HasValue)
        {
            var cst = await _cstService.GetAssignmentByIdAsync(request.ClassSubjectTeacherId.Value);
            if (cst.IsSuccess && cst.Data is not null)
                cstData = cst.Data;
        }

        // If no CST but we have SubjectId, build minimal context for the AI
        if (cstData is null && request.SubjectId.HasValue)
        {
            var subject = await _unitOfWork.Subjects.GetByIdAsync(request.SubjectId.Value);
            if (subject is not null && !subject.IsDeleted)
            {
                cstData = new ClassSubjectTeacherDto
                {
                    SubjectId = subject.Id,
                    SubjectName = subject.Name,
                };
            }
        }

        var context = cstData is not null
            ? await BuildContextAsync(request, cstData)
            : new ExamGenerationContext
            {
                SubjectName = "",
                ClassName = "",
                ContextText = ""
            };

        var typeNames = new Dictionary<int, string>
        {
            { 1, "اختيار من متعدد" },
            { 2, "صح أو خطأ" },
            { 3, "أكمل الفراغ" },
            { 4, "سؤال مقالي" }
        };

        var countsStr = string.Join(" و ",
            request.QuestionCounts
                .Where(kv => kv.Value > 0)
                .Select(kv => $"{kv.Value} {typeNames.GetValueOrDefault(kv.Key, $"نوع {kv.Key}")}"));

        var userPrompt = $$"""
                          أنت مدرس خبير في مادة "{{context.SubjectName}}".

                          المطلوب: إنشاء امتحان تقييم بعنوان "{{request.Title}}".
                          تفاصيل:
                          - المادة: {{context.SubjectName}} ({{context.ClassName}})
                          - الأسئلة: {{countsStr}}
                          - الدرجة الكلية: {{request.TotalScore}}
                          - المدة: {{request.DurationMinutes ?? 60}} دقيقة
                          """;

        if (!string.IsNullOrWhiteSpace(request.Topic))
            userPrompt += $"\n- الموضوع: {request.Topic}";

        if (!string.IsNullOrWhiteSpace(context.ContextText))
            userPrompt += $"\n\nالمحتوى الدراسي:\n{context.ContextText}";

        userPrompt += """
                     
                     مهمتك:
                     1. أنشئ أسئلة متنوعة ودقيقة تغطي المحتوى المطلوب
                     2. لكل سؤال قدم: نص السؤال، النوع، الإجابة الصحيحة، والدرجة
                     3. لأسئلة الاختيار من متعدد: قدم 4 خيارات (واحدة صحيحة)
                     4. لأسئلة صح/خطأ: حدد الصواب والخطأ مع الإجابة
                     5. لأسئلة أكمل: اكتب جملة مع فراغ
                     6. للأسئلة المقالية: اكتب السؤال فقط (بدون خيارات)

                     أرجع النتيجة بصيغة JSON فقط (بدون أي نص إضافي):
                     {
                       "title": "{{request.Title}}",
                       "durationMinutes": {{request.DurationMinutes ?? 60}},
                       "totalScore": {{request.TotalScore}},
                       "standaloneQuestions": [
                         {
                           "questionText": "نص السؤال",
                           "questionType": 1,
                           "options": [
                             { "text": "الخيار أ", "isCorrect": true, "displayOrder": 1 },
                             { "text": "الخيار ب", "isCorrect": false, "displayOrder": 2 },
                             { "text": "الخيار ج", "isCorrect": false, "displayOrder": 3 },
                             { "text": "الخيار د", "isCorrect": false, "displayOrder": 4 }
                           ],
                           "correctAnswer": "الإجابة الصحيحة",
                           "points": 5,
                           "displayOrder": 1
                         }
                       ]
                     }

                     قيم questionType:
                     - 1 = اختيار من متعدد (مع خيارات)
                     - 2 = صح أو خطأ (مع خيارات "صح" و"خطأ")
                     - 3 = أكمل الفراغ (بدون خيارات، correctAnswer هو الكلمة المفقودة)
                     - 4 = سؤال مقالي (بدون خيارات)
                     """;

        _logger.LogInformation("Sending exam generation prompt to LLM");

        var messages = new List<LlmChatMessage>
        {
            new(MessageRole.System, "أنت مساعد متخصص في إنشاء الامتحانات التعليمية. أخرج JSON فقط."),
            new(MessageRole.User, userPrompt)
        };

        LlmResponse llmResp;
        try
        {
            llmResp = await _llmClient.ChatAsync(messages, Enumerable.Empty<FunctionDefinition>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM call failed for exam generation");
            return OperationResult<(CreateExamFromAiDto, ClassSubjectTeacherDto?)>.Failure($"فشل الاتصال بالذكاء الاصطناعي: {ex.Message}");
        }

        var content = llmResp.Content;
        if (string.IsNullOrWhiteSpace(content))
            return OperationResult<(CreateExamFromAiDto, ClassSubjectTeacherDto?)>.Failure("لم يتم توليد الامتحان");

        content = content.Trim();
        if (content.StartsWith("```json"))
            content = content["```json".Length..];
        else if (content.StartsWith("```"))
            content = content["```".Length..];
        if (content.EndsWith("```"))
            content = content[..^"```".Length];
        content = content.Trim();

        _logger.LogDebug("LLM raw response: {Resp}", content[..Math.Min(content.Length, 500)]);

        AiGeneratedExamDto? generatedExam;
        try
        {
            generatedExam = JsonSerializer.Deserialize<AiGeneratedExamDto>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse LLM JSON. Response: {Resp}", content);
            return OperationResult<(CreateExamFromAiDto, ClassSubjectTeacherDto?)>.Failure("فشل تحليل JSON المولد");
        }

        if (generatedExam is null || generatedExam.StandaloneQuestions is null || generatedExam.StandaloneQuestions.Count == 0)
            return OperationResult<(CreateExamFromAiDto, ClassSubjectTeacherDto?)>.Failure("لم يتم توليد أي أسئلة");

        var createDto = new CreateExamFromAiDto
        {
            ClassSubjectTeacherId = request.ClassSubjectTeacherId,
            Title = generatedExam.Title ?? request.Title,
            DurationMinutes = generatedExam.DurationMinutes ?? request.DurationMinutes,
            TotalScore = generatedExam.TotalScore > 0 ? generatedExam.TotalScore : request.TotalScore,
            Category = request.Category,
            StandaloneQuestions = generatedExam.StandaloneQuestions.Select((q, i) => new AiQuestionDto
            {
                QuestionText = q.QuestionText,
                QuestionType = (QuestionType)q.QuestionType,
                Options = q.Options?.Select(o => new AiOptionDto
                {
                    Text = o.Text,
                    IsCorrect = o.IsCorrect,
                    DisplayOrder = o.DisplayOrder
                }).ToList(),
                CorrectAnswer = q.CorrectAnswer,
                Points = q.Points > 0 ? q.Points : (request.TotalScore / generatedExam.StandaloneQuestions.Count),
                DisplayOrder = i + 1
            }).ToList()
        };

        return OperationResult<(CreateExamFromAiDto, ClassSubjectTeacherDto?)>.Success((createDto, cstData));
    }

    private async Task<ExamGenerationContext> BuildContextAsync(
        AiGenerateExamRequest request, ClassSubjectTeacherDto cst)
    {
        var ctx = new ExamGenerationContext
        {
            SubjectName = cst.SubjectName,
            ClassName = cst.ClassName,
            ContextText = ""
        };

        // Fetch all units+lessons for the subject once (like book-parser does)
        var unitResult = await _unitService.GetUnitsWithLessonsBySubjectAsync(cst.SubjectId);
        var allUnits = unitResult.IsSuccess ? unitResult.Data : null;
        if (allUnits is null || allUnits.Count == 0) return ctx;

        if (request.UnitId.HasValue)
        {
            var unit = allUnits.FirstOrDefault(u => u.Id == request.UnitId.Value);
            if (unit is not null)
            {
                ctx.ContextText = $"الوحدة: {unit.Name}\n{unit.Content ?? ""}\n";
                if (request.LessonIds.Count > 0 && unit.Lessons is not null)
                {
                    var lessons = unit.Lessons.Where(l => request.LessonIds.Contains(l.Id)).ToList();
                    if (lessons.Count > 0)
                        ctx.ContextText += "الدروس:\n" + string.Join("\n---\n",
                            lessons.Select(l => $"الدرس: {l.Title}\n{l.Content ?? ""}"));
                }
                else if (unit.Lessons is not null)
                {
                    ctx.ContextText += "الدروس:\n" + string.Join("\n---\n",
                        unit.Lessons.Select(l => $"الدرس: {l.Title}\n{l.Content ?? ""}"));
                }
                return ctx;
            }
        }

        if (request.LessonIds.Count > 0)
        {
            var selected = allUnits
                .Where(u => u.Lessons is not null)
                .SelectMany(u => u.Lessons!)
                .Where(l => request.LessonIds.Contains(l.Id))
                .ToList();
            if (selected.Count > 0)
            {
                ctx.ContextText = string.Join("\n---\n",
                    selected.Select(l => $"الدرس: {l.Title}\n{l.Content ?? ""}"));
                return ctx;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Topic))
        {
            var matched = allUnits
                .Where(u => u.Lessons is not null)
                .SelectMany(u => u.Lessons!)
                .Where(l => !string.IsNullOrWhiteSpace(l.Content) &&
                    (l.Title.Contains(request.Topic, StringComparison.OrdinalIgnoreCase) ||
                     (l.Content?.Contains(request.Topic, StringComparison.OrdinalIgnoreCase) ?? false)))
                .Take(3)
                .ToList();
            if (matched.Count > 0)
            {
                ctx.ContextText = string.Join("\n---\n",
                    matched.Select(l => $"الدرس: {l.Title}\n{l.Content}"));
                return ctx;
            }
        }

        // Fallback: send all units/lessons from DB
        var parts = new List<string>();
        foreach (var unit in allUnits)
        {
            var unitText = $"الوحدة: {unit.Name}";
            if (!string.IsNullOrWhiteSpace(unit.Content))
                unitText += $"\n{unit.Content}";
            if (unit.Lessons is not null && unit.Lessons.Count > 0)
                unitText += "\nالدروس:\n" + string.Join("\n---\n",
                    unit.Lessons.Select(l => $"الدرس: {l.Title}\n{l.Content ?? ""}"));
            parts.Add(unitText);
        }
        ctx.ContextText = string.Join("\n\n===\n\n", parts);

        if (ctx.ContextText.Length > 8000)
            ctx.ContextText = ctx.ContextText[..8000] + "\n...";

        return ctx;
    }

    private class ExamGenerationContext
    {
        public string SubjectName { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string ContextText { get; set; } = "";
    }
}

public class AiGeneratedExamDto
{
    public string? Title { get; set; }
    public int? DurationMinutes { get; set; }
    public decimal TotalScore { get; set; }
    public List<AiGeneratedQuestionDto>? StandaloneQuestions { get; set; }
}

public class AiGeneratedQuestionDto
{
    public string QuestionText { get; set; } = "";
    public int QuestionType { get; set; }
    public List<AiGeneratedOptionDto>? Options { get; set; }
    public string? CorrectAnswer { get; set; }
    public decimal Points { get; set; }
    public int DisplayOrder { get; set; }
}

public class AiGeneratedOptionDto
{
    public string Text { get; set; } = "";
    public bool IsCorrect { get; set; }
    public int DisplayOrder { get; set; }
}
