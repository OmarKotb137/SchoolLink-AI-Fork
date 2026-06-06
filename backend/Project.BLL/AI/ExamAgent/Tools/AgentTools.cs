using System.Text.Json;
using Microsoft.Extensions.Logging;
using Project.BLL.AI.ExamAgent.Interfaces;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;
using Project.BLL.AI.ExamAgent.Models;
using Project.BLL.DTOs.Exam;
using Project.BLL.Interfaces;

namespace Project.BLL.AI.ExamAgent.Tools;

public class GetLessonsTool : IAgentTool
{
    private readonly ILessonRepository _repo;
    public GetLessonsTool(ILessonRepository repo) => _repo = repo;

    public string Name => "get_lessons";
    public string Description => "Retrieves available lessons. Optionally filter by subject (e.g. 'فيزياء', 'رياضيات').";

    public FunctionDefinition ToFunctionDefinition() => new()
    {
        Name = Name,
        Description = Description,
        InputSchema = new
        {
            type = "object",
            properties = new
            {
                subject = new
                {
                    type = "string",
                    description = "Optional subject filter, e.g. 'فيزياء'. Leave empty to list all lessons."
                }
            },
            required = Array.Empty<string>()
        }
    };

    public async Task<ToolResult> ExecuteAsync(JsonElement args)
    {
        string? subject = null;
        if (args.TryGetProperty("subject", out var s))
            subject = s.GetString();

        var lessons = await _repo.SearchAsync(subject);
        var summary = lessons.Select(l => new { l.Id, l.Title, l.Subject });
        return ToolResult.Ok(summary);
    }
}

public class GetLessonContentTool : IAgentTool
{
    private readonly ILessonRepository _repo;
    public GetLessonContentTool(ILessonRepository repo) => _repo = repo;

    public string Name => "get_lesson_content";
    public string Description =>
        "Retrieves the FULL content of a specific lesson by its ID. " +
        "You MUST call this before generate_exam so the exam is grounded in real content.";

    public FunctionDefinition ToFunctionDefinition() => new()
    {
        Name = Name,
        Description = Description,
        InputSchema = new
        {
            type = "object",
            properties = new
            {
                lessonId = new
                {
                    type = "integer",
                    description = "The numeric ID of the lesson (get it from get_lessons first)."
                }
            },
            required = new[] { "lessonId" }
        }
    };

    public async Task<ToolResult> ExecuteAsync(JsonElement args)
    {
        if (!args.TryGetProperty("lessonId", out var idEl))
            return ToolResult.Fail("lessonId is required.");

        var lesson = await _repo.GetByIdAsync(idEl.GetInt32());
        return lesson is null
            ? ToolResult.Fail($"Lesson with id {idEl.GetInt32()} not found.")
            : ToolResult.Ok(new { lesson.Id, lesson.Title, lesson.Subject, lesson.Content });
    }
}

public class GenerateExamTool : IAgentTool
{
    private readonly IExamGenerator _gen;
    private readonly IExamService _examService;
    private readonly ILogger<GenerateExamTool> _logger;

    public GenerateExamTool(IExamGenerator gen, IExamService examService, ILogger<GenerateExamTool> logger)
    {
        _gen = gen;
        _examService = examService;
        _logger = logger;
    }

    public string Name => "generate_exam";
    public string Description =>
        "Generates an exam based on lesson content. " +
        "REQUIRES lessonContent — call get_lesson_content first if you don't have it yet.";

    public FunctionDefinition ToFunctionDefinition() => new()
    {
        Name = Name,
        Description = Description,
        InputSchema = new
        {
            type = "object",
            properties = new
            {
                lessonContent = new
                {
                    type = "string",
                    description = "The full text of the lesson. Do NOT summarize — pass the whole content."
                },
                questionCount = new
                {
                    type = "integer",
                    description = "Number of questions to generate.",
                    @default = 5
                },
                classSubjectTeacherId = new
                {
                    type = "integer",
                    description = "ClassSubjectTeacher ID. If not provided, uses default."
                },
                difficulty = new
                {
                    type = "string",
                    @enum = new[] { "easy", "medium", "hard" },
                    description = "Difficulty level of the exam."
                },
                style = new
                {
                    type = "string",
                    @enum = new[] { "multiple_choice", "true_false", "open_ended" },
                    description = "Question style."
                }
            },
            required = new[] { "lessonContent", "questionCount", "difficulty", "style" }
        }
    };

    public async Task<ToolResult> ExecuteAsync(JsonElement args)
    {
        if (!args.TryGetProperty("lessonContent", out var contentEl)
            || string.IsNullOrWhiteSpace(contentEl.GetString()))
        {
            return ToolResult.Fail(
                "Cannot generate exam without lessonContent. " +
                "Call get_lesson_content first to retrieve the lesson text.");
        }

        var request = new ExamRequest
        {
            LessonContent = contentEl.GetString()!,
            QuestionCount = args.TryGetProperty("questionCount", out var qc) ? qc.GetInt32() : 5,
            Difficulty = args.TryGetProperty("difficulty", out var d) ? d.GetString()! : "medium",
            Style = args.TryGetProperty("style", out var st) ? st.GetString()! : "multiple_choice"
        };

        var examResp = await _gen.GenerateAsync(request);
        if (string.IsNullOrEmpty(examResp.Content) || examResp.Content == "لم يتم توليد الامتحان.")
            return ToolResult.Fail("لم يتم توليد الامتحان.");

        // Deserialize the JSON into CreateExamFromAiDto
        try
        {
            var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var aiDto = JsonSerializer.Deserialize<CreateExamFromAiDto>(examResp.Content, jsonOpts);
            if (aiDto is null)
                return ToolResult.Fail("فشل تحليل JSON المولد.");

            aiDto.ClassSubjectTeacherId = args.TryGetProperty("classSubjectTeacherId", out var cst)
                ? cst.GetInt32() : 1;
            if (string.IsNullOrWhiteSpace(aiDto.Title))
                aiDto.Title = "امتحان غير محدد";

            var saved = await _examService.CreateFromAiAsync(aiDto);
            if (!saved.IsSuccess)
                return ToolResult.Fail($"فشل حفظ الامتحان: {saved.Message}");

            var exam = saved.Data;
            return ToolResult.Ok(new
            {
                examId = exam?.Id,
                title = exam?.Title,
                totalScore = exam?.TotalScore,
                questionCount = exam?.QuestionsCount ?? 0,
                message = "تم إنشاء الامتحان وحفظه بنجاح"
            });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse LLM exam JSON");
            return ToolResult.Fail($"خطأ في تحليل JSON: {ex.Message}");
        }
    }
}
