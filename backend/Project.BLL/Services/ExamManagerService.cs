using Common.Results;
using Microsoft.EntityFrameworkCore;
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

    public async Task<OperationResult<List<ExamManagerItemDto>>> GetAllAsync(int? cstId = null)
    {
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

        if (cstId.HasValue)
            query = query.Where(e => e.ClassSubjectTeacherId == cstId.Value);

        var exams = await query.OrderByDescending(e => e.CreatedAt).ToListAsync();

        var dtos = exams.Select(e =>
        {
            var now = DateTime.UtcNow;
            var status = GetStatus(e, now);
            var questionCount = e.Questions.Count + e.Groups.Sum(g => g.Questions.Count);
            var classEntity = e.ClassSubjectTeacher?.Class;
            var className = classEntity != null
                ? $"{classEntity.GradeLevel.Name} - {classEntity.Name}"
                : "";
            var subjectName = e.Subject?.Name ?? e.ClassSubjectTeacher?.Subject.Name ?? "";
            var avgScore = e.Attempts.Count > 0
                ? Math.Round(e.Attempts.Where(a => a.Score.HasValue).Average(a => (double)(a.Score.Value / (a.TotalScore > 0 ? a.TotalScore : 1) * 100)), 1)
                : (double?)null;
            var totalStudents = classEntity?.Enrollments.Count(e => !e.IsDeleted) ?? 0;

            return new ExamManagerItemDto(
                e.Id, e.Title, subjectName, className,
                e.StartTime?.ToString("yyyy-MM-dd") ?? "",
                e.StartTime?.ToString("HH:mm") ?? "",
                e.EndTime?.ToString("HH:mm") ?? "",
                e.DurationMinutes ?? 0,
                questionCount, status,
                avgScore, e.Attempts.Count, totalStudents > 0 ? totalStudents : null
            );
        }).ToList();

        return OperationResult<List<ExamManagerItemDto>>.Success(dtos);
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
            questionCount, status, questions
        );

        return OperationResult<ExamManagerDetailDto>.Success(dto);
    }

    public async Task<OperationResult<ExamManagerStatsDto>> GetStatsAsync(int? cstId = null)
    {
        var query = _context.Exams
            .Include(e => e.Attempts)
            .Where(e => !e.IsDeleted)
            .AsQueryable();

        if (cstId.HasValue)
            query = query.Where(e => e.ClassSubjectTeacherId == cstId.Value);

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
            TotalScore = 100,
            IsPublished = false,
            Category = EvaluationCategory.Academic,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Exams.AddAsync(exam);
        await _unitOfWork.SaveChangesAsync();

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
        exam.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Exams.Update(exam);
        await _unitOfWork.SaveChangesAsync();

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
        var exam = await _unitOfWork.Exams.GetByIdAsync(id);
        if (exam == null || exam.IsDeleted)
            return OperationResult.Failure("الامتحان غير موجود");

        exam.IsPublished = true;
        exam.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Exams.Update(exam);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("تم نشر الامتحان بنجاح");
    }

    private static string GetStatus(Exam exam, DateTime nowUnused)
    {
        var now = DateTime.UtcNow.AddHours(3); // Adjust to Egypt Standard Time for comparison
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

        return new ExamManagerQuestionDto(q.Id, type, q.QuestionText, options, correctAnswer);
    }
}
