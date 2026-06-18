using Common.Results;
using Microsoft.EntityFrameworkCore;
using Project.BLL.DTOs.Common;
using Project.BLL.DTOs.Exam;
using Project.BLL.Interfaces;
using Project.DAL.Context;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.Services;

public class ExamManagerService : IExamManagerService
{
    private readonly AppDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public ExamManagerService(AppDbContext context, IUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    public async Task<OperationResult<PagedResult<ExamManagerItemDto>>> GetAllAsync(ExamManagerFilterDto filter)
    {
        var cairoZone = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "Egypt Standard Time" : "Africa/Cairo"
        );
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, cairoZone);

        var query = _context.Exams
            .Include(e => e.Subject)
            .Include(e => e.ClassSubjectTeacher).ThenInclude(cst => cst!.Subject)
            .Include(e => e.ClassSubjectTeacher).ThenInclude(cst => cst!.Class).ThenInclude(c => c.GradeLevel)
            .Include(e => e.ClassSubjectTeacher).ThenInclude(cst => cst!.Class).ThenInclude(c => c.Enrollments)
            .Include(e => e.Attempts)
            .Include(e => e.Questions)
            .Include(e => e.Groups).ThenInclude(g => g.Questions)
            .Where(e => !e.IsDeleted)
            .AsQueryable();

        // Filter by CST IDs (academic year)
        if (filter.CstIds != null && filter.CstIds.Count > 0)
            query = query.Where(e => filter.CstIds.Contains(e.ClassSubjectTeacherId!.Value));

        // Filter by subject
        if (filter.SubjectId.HasValue)
            query = query.Where(e => e.SubjectId == filter.SubjectId.Value);

        // Filter by search term
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            query = query.Where(e => e.Title.ToLower().Contains(term));
        }

        // Apply sorting before fetching
        query = (filter.SortBy?.ToLower()) switch
        {
            "oldest" => query.OrderBy(e => e.CreatedAt),
            "name-asc" => query.OrderBy(e => e.Title),
            "name-desc" => query.OrderByDescending(e => e.Title),
            "date-asc" => query.OrderBy(e => e.StartTime ?? e.CreatedAt),
            "date-desc" => query.OrderByDescending(e => e.StartTime ?? e.CreatedAt),
            _ => query.OrderByDescending(e => e.CreatedAt), // "newest" or default
        };

        // Fetch all matching exams for in-memory status filtering
        var exams = await query.ToListAsync();

        // Apply status filter in memory (since status is computed at runtime)
        if (!string.IsNullOrWhiteSpace(filter.Status) && filter.Status != "all")
        {
            exams = exams.Where(e => GetStatus(e, now) == filter.Status).ToList();
        }

        var totalCount = exams.Count;

        // Apply pagination in memory after status filter
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Max(1, Math.Min(100, filter.PageSize));
        var pagedExams = exams
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var dtos = pagedExams.Select(e =>
        {
            var status = GetStatus(e, now);
            var questionCount = e.Questions.Count + e.Groups.Sum(g => g.Questions.Count);
            var classEntity = e.ClassSubjectTeacher?.Class;
            var className = classEntity != null
                ? $"{classEntity.GradeLevel.Name} - {classEntity.Name}"
                : "";
            var subjectName = e.Subject?.Name ?? e.ClassSubjectTeacher?.Subject.Name ?? "";
            var scoredAttempts = e.Attempts.Where(a => a.Score.HasValue).ToList();
            var avgScore = scoredAttempts.Count > 0
                ? Math.Round(scoredAttempts.Average(a => (double)(a.Score!.Value / (a.TotalScore > 0 ? a.TotalScore : 1) * 100)), 1)
                : (double?)null;
            var totalStudents = classEntity?.Enrollments.Count(e => !e.IsDeleted) ?? 0;
            var pendingGrading = e.Attempts.Count(a => a.SubmittedAt != null && !a.IsGraded);

            return new ExamManagerItemDto(
                e.Id, e.Title, subjectName, className,
                e.StartTime?.ToString("yyyy-MM-dd") ?? "",
                e.StartTime?.ToString("HH:mm") ?? "",
                e.EndTime?.ToString("HH:mm") ?? "",
                e.DurationMinutes ?? 0,
                questionCount, status,
                avgScore, e.Attempts.Count, totalStudents > 0 ? totalStudents : null,
                e.IsResultPublished,
                pendingGrading > 0 ? pendingGrading : null,
                e.IsAIGenerated
            );
        }).ToList();

        var paged = new PagedResult<ExamManagerItemDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };

        return OperationResult<PagedResult<ExamManagerItemDto>>.Success(paged);
    }

    public async Task<OperationResult<ExamManagerDetailDto>> GetByIdAsync(int id)
    {
        var exam = await _context.Exams
            .Include(e => e.Subject)
            .Include(e => e.ClassSubjectTeacher).ThenInclude(cst => cst!.Subject)
            .Include(e => e.ClassSubjectTeacher).ThenInclude(cst => cst!.Class).ThenInclude(c => c.GradeLevel)
            .Include(e => e.Questions).ThenInclude(q => q.Options)
            .Include(e => e.Groups).ThenInclude(g => g.Questions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

        if (exam == null)
            return OperationResult<ExamManagerDetailDto>.Failure("الامتحان غير موجود", 404);

        var now = DateTime.UtcNow;
        var status = GetStatus(exam, now);
        var questionCount = exam.Questions.Count + exam.Groups.Sum(g => g.Questions.Count);
        var classEntity = exam.ClassSubjectTeacher?.Class;
        var className = classEntity != null
            ? $"{classEntity.GradeLevel.Name} - {classEntity.Name}"
            : "";
        var subjectName = exam.Subject?.Name ?? exam.ClassSubjectTeacher?.Subject.Name ?? "";

        var questions = new List<ExamManagerQuestionDto>();

        foreach (var q in exam.Questions.OrderBy(q => q.DisplayOrder))
        {
            questions.Add(MapQuestion(q));
        }

        foreach (var group in exam.Groups.OrderBy(g => g.DisplayOrder))
        {
            foreach (var q in group.Questions.OrderBy(q => q.DisplayOrder))
            {
                questions.Add(MapQuestion(q));
            }
        }

        var dto = new ExamManagerDetailDto(
            exam.Id, exam.Title, subjectName, className,
            exam.StartTime?.ToString("yyyy-MM-dd") ?? "",
            exam.StartTime?.ToString("HH:mm") ?? "",
            exam.EndTime?.ToString("HH:mm") ?? "",
            exam.DurationMinutes ?? 0,
            questionCount, status, exam.IsResultPublished,
            exam.TotalScore, questions
        );

        return OperationResult<ExamManagerDetailDto>.Success(dto);
    }

    public async Task<OperationResult<ExamManagerStatsDto>> GetStatsAsync(List<int>? cstIds = null)
    {
        var query = _context.Exams
            .Include(e => e.Attempts)
            .Where(e => !e.IsDeleted)
            .AsQueryable();

        if (cstIds != null && cstIds.Count > 0)
            query = query.Where(e => cstIds.Contains(e.ClassSubjectTeacherId!.Value));

        var exams = await query.ToListAsync();
        var now = DateTime.UtcNow;

        var total = exams.Count;
        var upcoming = exams.Count(e => GetStatus(e, now) == "upcoming");
        var ended = exams.Count(e => GetStatus(e, now) == "ended");
        var avgScore = exams.SelectMany(e => e.Attempts)
            .Where(a => a.Score.HasValue && a.TotalScore > 0)
            .Select(a => (double)(a.Score!.Value / a.TotalScore * 100))
            .DefaultIfEmpty(0)
            .Average();

        var stats = new ExamManagerStatsDto(total, upcoming, ended, Math.Round(avgScore, 1));
        return OperationResult<ExamManagerStatsDto>.Success(stats);
    }

    public async Task<OperationResult<ExamManagerDetailDto>> CreateAsync(CreateExamManagerDto dto, int teacherId)
    {
        var classEntity = await _context.Classes
            .Include(c => c.GradeLevel)
            .FirstOrDefaultAsync(c => c.Id == dto.ClassId && !c.IsDeleted);

        if (classEntity == null)
            return OperationResult<ExamManagerDetailDto>.Failure("الفصل غير موجود", 404);

        var cst = await _context.ClassSubjectTeachers
            .Include(c => c.Subject)
            .Include(c => c.Class).ThenInclude(c => c.GradeLevel)
            .FirstOrDefaultAsync(c =>
                c.ClassId == dto.ClassId &&
                c.SubjectId == dto.SubjectId &&
                c.TeacherId == teacherId &&
                c.AcademicYearId == classEntity.AcademicYearId &&
                !c.IsDeleted);

        if (cst == null)
            return OperationResult<ExamManagerDetailDto>.Failure("لا يوجد تدريس لهذه المادة في هذا الفصل", 400);

        var startTime = DateTime.Parse(dto.Date + " " + dto.StartTime);
        var endTime = DateTime.Parse(dto.Date + " " + dto.EndTime);

        var exam = new Exam
        {
            ClassSubjectTeacherId = cst.Id,
            SubjectId = dto.SubjectId,
            GradeLevelId = cst.Class.GradeLevelId,
            Title = dto.Title,
            StartTime = startTime,
            EndTime = endTime,
            DurationMinutes = dto.DurationMinutes,
            TotalScore = dto.TotalScore,
            IsPublished = false,
            Category = EvaluationCategory.Academic,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Exams.AddAsync(exam);
        await _unitOfWork.SaveChangesAsync();

        // حفظ الأسئلة اليدوية لو وُجدت
        if (dto.Questions != null)
            await SaveQuestionsAsync(exam.Id, dto.Questions);

        var detail = await GetByIdAsync(exam.Id);
        return detail;
    }

    public async Task<OperationResult> UpdateAsync(int id, CreateExamManagerDto dto)
    {
        var exam = await _unitOfWork.Exams.GetByIdAsync(id);
        if (exam == null || exam.IsDeleted)
            return OperationResult.Failure("الامتحان غير موجود");

        var startTime = DateTime.Parse(dto.Date + " " + dto.StartTime);
        var endTime = DateTime.Parse(dto.Date + " " + dto.EndTime);

        exam.Title = dto.Title;
        exam.StartTime = startTime;
        exam.EndTime = endTime;
        exam.DurationMinutes = dto.DurationMinutes;
        exam.TotalScore = dto.TotalScore;
        exam.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Exams.Update(exam);
        await _unitOfWork.SaveChangesAsync();

        // إعادة بناء الأسئلة لو أُرسلت (soft-replace: احذف القديمة وأضف الجديدة)
        if (dto.Questions != null)
        {
            // حذف أسئلة الامتحان المباشرة فقط (ليس المجموعات)
            var oldQuestions = await _context.ExamQuestions
                .Where(q => q.ExamId == id && !q.IsDeleted)
                .ToListAsync();
            foreach (var q in oldQuestions)
            {
                q.IsDeleted = true;
                q.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            await SaveQuestionsAsync(id, dto.Questions);
        }

        return OperationResult.Success("تم تحديث الامتحان بنجاح");
    }

    public async Task<OperationResult> DeleteAsync(int id)
    {
        var exam = await _unitOfWork.Exams.GetByIdAsync(id);
        if (exam == null || exam.IsDeleted)
            return OperationResult.Failure("الامتحان غير موجود");

        _unitOfWork.Exams.SoftDelete(exam);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("تم حذف الامتحان بنجاح");
    }

    public async Task<OperationResult> PublishAsync(int id, int teacherId)
    {
        var exam = await _context.Exams
            .Include(e => e.ClassSubjectTeacher)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

        if (exam is null)
            return OperationResult.Failure("الامتحان غير موجود", 404);

        if (exam.ClassSubjectTeacher?.TeacherId != teacherId)
            return OperationResult.Failure("غير مصرح لك بنشر هذا الامتحان", 403);

        exam.IsPublished = true;
        exam.UpdatedAt = DateTime.UtcNow;

        _context.Exams.Update(exam);
        await _context.SaveChangesAsync();

        return OperationResult.Success("تم نشر الامتحان بنجاح");
    }

    public async Task<OperationResult> ToggleResultPublishStatusAsync(int id, bool isPublished, int teacherId)
    {
        var exam = await _context.Exams
            .Include(e => e.ClassSubjectTeacher)
            .Include(e => e.Attempts)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

        if (exam is null)
            return OperationResult.Failure("الامتحان غير موجود", 404);

        if (exam.ClassSubjectTeacher?.TeacherId != teacherId)
            return OperationResult.Failure("غير مصرح لك بتعديل هذا الامتحان", 403);

        // Phase 6.4: منع النشر لو فيه محاولات لم تُصحح بعد
        if (isPublished)
        {
            bool hasPending = exam.Attempts.Any(a => a.SubmittedAt != null && !a.IsGraded);
            if (hasPending)
                return OperationResult.Failure("لا يمكن نشر النتائج، يوجد إجابات لم تُصحح بعد", 400);
        }

        exam.IsResultPublished = isPublished;
        exam.UpdatedAt = DateTime.UtcNow;

        _context.Exams.Update(exam);
        await _context.SaveChangesAsync();

        return OperationResult.Success();
    }

    private static string GetStatus(Exam exam, DateTime _ )
    {
        // Phase 5: استخدام TimeZoneInfo بدل من AddHours هاردكود
        var cairoZone = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "Egypt Standard Time" : "Africa/Cairo"
        );
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, cairoZone);

        if (!exam.IsPublished)
            return "draft";
        if (exam.StartTime == null || exam.EndTime == null)
            return "draft";
        if (now < exam.StartTime)
            return "upcoming";
        if (now >= exam.StartTime && now <= exam.EndTime)
            return "active";
        return "ended";
    }

    // Phase 3: حفظ أسئلة يدوية جديدة لامتحان
    private async Task SaveQuestionsAsync(int examId, List<CreateExamManagerQuestionDto> questions)
    {
        var now = DateTime.UtcNow;
        int order = 1;

        foreach (var q in questions)
        {
            var qType = q.Type switch
            {
                "mcq"        => QuestionType.MultipleChoice,
                "true-false" => QuestionType.TrueFalse,
                _            => QuestionType.Essay,
            };

            var question = new ExamQuestion
            {
                ExamId       = examId,
                QuestionText = q.Text,
                QuestionType = qType,
                CorrectAnswer = q.CorrectAnswer ?? "",
                Points       = q.Points,
                DisplayOrder = order++,
                CreatedAt    = now,
                UpdatedAt    = now,
            };
            _context.ExamQuestions.Add(question);
            await _context.SaveChangesAsync();

            if ((qType == QuestionType.MultipleChoice || qType == QuestionType.TrueFalse)
                && q.Options?.Count > 0)
            {
                int optOrder = 1;
                foreach (var opt in q.Options)
                {
                    _context.ExamQuestionOptions.Add(new ExamQuestionOption
                    {
                        QuestionId   = question.Id,
                        OptionText   = opt,
                        IsCorrect    = opt == q.CorrectAnswer,
                        DisplayOrder = optOrder++,
                        CreatedAt    = now,
                        UpdatedAt    = now,
                    });
                }
                await _context.SaveChangesAsync();
            }
        }
    }

    private static ExamManagerQuestionDto MapQuestion(ExamQuestion q)
    {
        var type = q.QuestionType switch
        {
            QuestionType.MultipleChoice => "mcq",
            QuestionType.TrueFalse => "true-false",
            QuestionType.Essay => "essay",
            _ => "mcq"
        };

        List<string>? options = null;
        string correctAnswer = "";

        if (q.QuestionType == QuestionType.MultipleChoice || q.QuestionType == QuestionType.TrueFalse)
        {
            options = q.Options.OrderBy(o => o.DisplayOrder).Select(o => o.OptionText).ToList();
            var correct = q.Options.FirstOrDefault(o => o.IsCorrect);
            correctAnswer = correct?.OptionText ?? "";
        }

        return new ExamManagerQuestionDto(q.Id, type, q.QuestionText, options, correctAnswer, q.Points);
    }
}
