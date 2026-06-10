using System.Text.Json;
using Common.Results;
using Microsoft.Extensions.Logging;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;
using Project.BLL.DTOs.Exam;
using Project.BLL.Interfaces;

namespace Project.BLL.AI.Services;

public class ExamGeneratorService : IExamGeneratorService
{
    private readonly IExamGenerator _gen;
    private readonly IExamService _examService;
    private readonly ILogger<ExamGeneratorService> _logger;

    public ExamGeneratorService(IExamGenerator gen, IExamService examService, ILogger<ExamGeneratorService> logger)
    {
        _gen = gen;
        _examService = examService;
        _logger = logger;
    }

    public async Task<OperationResult<GetExamDto>> GenerateExamAsync(CreateExamFromAiDto dto, CancellationToken ct = default)
    {
        var lessonContent = string.Join("\n", dto.Groups.SelectMany(g => g.Questions).Concat(dto.StandaloneQuestions)
            .Select(q => q.QuestionText));

        var request = new ExamRequest
        {
            LessonContent = string.IsNullOrWhiteSpace(lessonContent) ? "محتوى افتراضي" : lessonContent,
            QuestionCount = dto.StandaloneQuestions.Count + dto.Groups.Sum(g => g.Questions.Count),
            Difficulty = "medium",
            Style = "mixed"
        };

        var examResp = await _gen.GenerateAsync(request);

        if (string.IsNullOrEmpty(examResp.Content) || examResp.Content == "لم يتم توليد الامتحان.")
            return OperationResult<GetExamDto>.Failure("لم يتم توليد الامتحان.");

        try
        {
            var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var generatedDto = JsonSerializer.Deserialize<CreateExamFromAiDto>(examResp.Content, jsonOpts);
            if (generatedDto is null)
                return OperationResult<GetExamDto>.Failure("فشل تحليل JSON المولد.");

            generatedDto.ClassSubjectTeacherId = dto.ClassSubjectTeacherId;
            generatedDto.Title = dto.Title;
            generatedDto.TotalScore = dto.TotalScore;
            generatedDto.Category = dto.Category;
            generatedDto.DurationMinutes = dto.DurationMinutes;

            var saved = await _examService.CreateFromAiAsync(generatedDto, ct);
            return saved;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse LLM exam JSON");
            return OperationResult<GetExamDto>.Failure($"خطأ في تحليل JSON: {ex.Message}");
        }
    }

    public async Task<OperationResult<GetExamDto>> RegenerateQuestionsAsync(int examId, List<int> questionIds, CancellationToken ct = default)
    {
        var exam = await _examService.GetByIdAsync(examId);
        if (!exam.IsSuccess || exam.Data is null)
            return OperationResult<GetExamDto>.Failure("الامتحان غير موجود");

        var lessonContent = exam.Data.Title ?? "محتوى الامتحان";

        var request = new ExamRequest
        {
            LessonContent = lessonContent,
            QuestionCount = questionIds.Count,
            Difficulty = "medium",
            Style = "mixed"
        };

        var examResp = await _gen.GenerateAsync(request);
        if (string.IsNullOrEmpty(examResp.Content) || examResp.Content == "لم يتم توليد الامتحان.")
            return OperationResult<GetExamDto>.Failure("لم يتم توليد الامتحان.");

        try
        {
            var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var generatedDto = JsonSerializer.Deserialize<CreateExamFromAiDto>(examResp.Content, jsonOpts);
            if (generatedDto is null)
                return OperationResult<GetExamDto>.Failure("فشل تحليل JSON المولد.");

            generatedDto.Title = exam.Data.Title;
            generatedDto.TotalScore = exam.Data.TotalScore;

            var saved = await _examService.CreateFromAiAsync(generatedDto, ct);
            return saved;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse LLM exam JSON");
            return OperationResult<GetExamDto>.Failure($"خطأ في تحليل JSON: {ex.Message}");
        }
    }
}
