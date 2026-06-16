using System.Text.Json;
using Common.Results;
using Microsoft.Extensions.Logging;
using Project.BLL.DTOs.QuestionBank;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.Services;

public class QuestionBankService : IQuestionBankService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<QuestionBankService> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public QuestionBankService(IUnitOfWork unitOfWork, ILogger<QuestionBankService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<OperationResult<List<QuestionBankItemDto>>> GetBySubjectAsync(int subjectId, int? gradeLevelId = null)
    {
        var items = await _unitOfWork.QuestionBank
            .FindAsync(q => q.SubjectId == subjectId && !q.IsDeleted);

        var filtered = items.AsEnumerable();
        if (gradeLevelId.HasValue && gradeLevelId > 0)
            filtered = filtered.Where(q => q.GradeLevelId == gradeLevelId.Value);

        var dtos = filtered.Select(MapToDto).ToList();
        return OperationResult<List<QuestionBankItemDto>>.Success(dtos);
    }

    public async Task<OperationResult<QuestionBankItemDto>> GetByIdAsync(int id)
    {
        var item = await _unitOfWork.QuestionBank.GetByIdAsync(id);
        if (item is null || item.IsDeleted)
            return OperationResult<QuestionBankItemDto>.Failure("السؤال غير موجود", 404);

        return OperationResult<QuestionBankItemDto>.Success(MapToDto(item));
    }

    public async Task<OperationResult<List<QuestionBankItemDto>>> SearchAsync(SearchQuestionBankDto dto)
    {
        var allItems = await _unitOfWork.QuestionBank.FindAsync(q => !q.IsDeleted);

        var filtered = allItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(dto.SearchText))
        {
            var search = dto.SearchText.ToLower();
            filtered = filtered.Where(q => q.QuestionText.ToLower().Contains(search));
        }

        if (dto.SubjectId.HasValue)
            filtered = filtered.Where(q => q.SubjectId == dto.SubjectId.Value);

        if (dto.GradeLevelId.HasValue)
            filtered = filtered.Where(q => q.GradeLevelId == dto.GradeLevelId.Value);

        if (dto.QuestionType.HasValue)
            filtered = filtered.Where(q => (int)q.QuestionType == dto.QuestionType.Value);

        var total = filtered.Count();

        var items = filtered
            .OrderByDescending(q => q.UsageCount)
            .Skip((dto.Page - 1) * dto.PageSize)
            .Take(dto.PageSize)
            .ToList();

        var dtos = items.Select(MapToDto).ToList();
        return OperationResult<List<QuestionBankItemDto>>.Success(dtos,
            $"تم العثور على {total} سؤال");
    }

    public async Task<OperationResult<QuestionBankItemDto>> AddQuestionAsync(AddToQuestionBankDto dto)
    {
        if (dto.GradeLevelId > 0)
        {
            var gradeLevel = await _unitOfWork.GradeLevels.GetByIdAsync(dto.GradeLevelId);
            if (gradeLevel == null || gradeLevel.IsDeleted)
                return OperationResult<QuestionBankItemDto>.Failure("الصف الدراسي غير موجود", 404);
        }

        var existing = await _unitOfWork.QuestionBank
            .FindAsync(q => q.QuestionText == dto.QuestionText && q.SubjectId == dto.SubjectId && q.GradeLevelId == dto.GradeLevelId && !q.IsDeleted);

        var existingList = existing.ToList();
        if (existingList.Any())
        {
            var existingItem = existingList.First();
            existingItem.UsageCount++;
            _unitOfWork.QuestionBank.Update(existingItem);
            await _unitOfWork.SaveChangesAsync();

            return OperationResult<QuestionBankItemDto>.Success(MapToDto(existingItem),
                "السؤال موجود مسبقاً في بنك الأسئلة، تم زيادة عدد الاستخدامات");
        }

        var optionsJson = dto.Options?.Count > 0
            ? JsonSerializer.Serialize(dto.Options.Select(o => new
            {
                text = o.Text,
                isCorrect = o.IsCorrect,
                displayOrder = o.DisplayOrder
            }), JsonOpts)
            : null;

        var bankItem = new QuestionBank
        {
            QuestionText = dto.QuestionText,
            QuestionType = (QuestionType)dto.QuestionType,
            CorrectAnswer = dto.CorrectAnswer,
            OptionsJson = optionsJson,
            SubjectId = dto.SubjectId,
            GradeLevelId = dto.GradeLevelId,
            SourceExamId = dto.SourceExamId,
            UsageCount = 1
        };

        await _unitOfWork.QuestionBank.AddAsync(bankItem);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Added question to bank: {QuestionText} (Subject: {SubjectId})",
            dto.QuestionText[..Math.Min(dto.QuestionText.Length, 50)], dto.SubjectId);

        return OperationResult<QuestionBankItemDto>.Success(MapToDto(bankItem),
            "تم إضافة السؤال إلى بنك الأسئلة بنجاح");
    }

    public async Task<OperationResult<int>> BulkAddFromExamAsync(int examId, int subjectId)
    {
        var exam = await _unitOfWork.Exams.GetWithQuestionsAsync(examId);
        if (exam is null)
            return OperationResult<int>.Failure("الامتحان غير موجود", 404);

        var gradeLevelId = exam.GradeLevelId;
        var newCount = 0;
        var existingCount = 0;

        foreach (var question in exam.Questions.Where(q => !q.IsDeleted))
        {
            var existing = await _unitOfWork.QuestionBank
                .FindAsync(q => q.QuestionText == question.QuestionText && q.SubjectId == subjectId && q.GradeLevelId == gradeLevelId && !q.IsDeleted);

            QuestionBank bankItem;
            var existingList = existing.ToList();
            if (existingList.Any())
            {
                bankItem = existingList.First();
                bankItem.UsageCount++;
                _unitOfWork.QuestionBank.Update(bankItem);
                existingCount++;
            }
            else
            {
                var optionsJson = question.Options?.Count > 0
                    ? JsonSerializer.Serialize(question.Options.Select(o => new
                    {
                        text = o.OptionText,
                        isCorrect = o.IsCorrect,
                        displayOrder = o.DisplayOrder
                    }), JsonOpts)
                    : null;

                bankItem = new QuestionBank
                {
                    QuestionText = question.QuestionText,
                    QuestionType = question.QuestionType,
                    CorrectAnswer = question.CorrectAnswer,
                    OptionsJson = optionsJson,
                    SubjectId = subjectId,
                    GradeLevelId = gradeLevelId,
                    SourceExamId = examId,
                    UsageCount = 1
                };
                await _unitOfWork.QuestionBank.AddAsync(bankItem);
                newCount++;
            }

            await _unitOfWork.SaveChangesAsync();

            var linkExists = await _unitOfWork.ExamQuestionBankItems
                .FindAsync(l => l.ExamId == examId && l.QuestionBankId == bankItem.Id);

            if (!linkExists.Any())
            {
                await _unitOfWork.ExamQuestionBankItems.AddAsync(new ExamQuestionBankItem
                {
                    ExamId = examId,
                    QuestionBankId = bankItem.Id,
                    Points = question.Points,
                    DisplayOrder = question.DisplayOrder
                });
                await _unitOfWork.SaveChangesAsync();
            }
        }

        var totalCount = newCount + existingCount;

        _logger.LogInformation("Bulk added from exam {ExamId}: {NewCount} new, {ExistingCount} existing",
            examId, newCount, existingCount);

        if (newCount == 0)
        {
            return OperationResult<int>.Success(totalCount,
                $"جميع أسئلة الامتحان موجودة مسبقاً في بنك الأسئلة (تم زيادة عدد الاستخدامات لـ {existingCount} سؤال)");
        }

        return OperationResult<int>.Success(totalCount,
            existingCount > 0
                ? $"تم إضافة {newCount} سؤال جديد إلى بنك الأسئلة، و{existingCount} أسئلة موجودة مسبقاً (تم زيادة عدد الاستخدامات)"
                : $"تم إضافة {newCount} سؤال إلى بنك الأسئلة بنجاح");
    }

    public async Task<OperationResult> DeleteAsync(int id)
    {
        var item = await _unitOfWork.QuestionBank.GetByIdAsync(id);
        if (item is null || item.IsDeleted)
            return OperationResult.Failure("السؤال غير موجود", 404);

        _unitOfWork.QuestionBank.SoftDelete(item);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("تم حذف السؤال من بنك الأسئلة");
    }

    private static QuestionBankItemDto MapToDto(QuestionBank item)
    {
        var options = new List<QuestionBankOptionDto>();
        if (!string.IsNullOrWhiteSpace(item.OptionsJson))
        {
            try
            {
                var rawOptions = JsonSerializer.Deserialize<List<QuestionBankOptionDto>>(item.OptionsJson, JsonOpts);
                if (rawOptions is not null)
                    options = rawOptions;
            }
            catch { }
        }

        return new QuestionBankItemDto
        {
            Id = item.Id,
            QuestionText = item.QuestionText,
            QuestionType = (int)item.QuestionType,
            CorrectAnswer = item.CorrectAnswer,
            Options = options,
            SubjectId = item.SubjectId,
            GradeLevelId = item.GradeLevelId,
            GradeLevelName = item.GradeLevel?.Name,
            UsageCount = item.UsageCount,
            CreatedAt = item.CreatedAt
        };
    }
}
