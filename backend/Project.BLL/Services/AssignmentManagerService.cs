using Common.Results;
using Microsoft.EntityFrameworkCore;
using Project.BLL.Interfaces;
using Project.DAL.Context;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.Services;

public class AssignmentManagerService : IAssignmentManagerService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly AppDbContext _context;

    public AssignmentManagerService(IUnitOfWork unitOfWork, AppDbContext context)
    {
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task<OperationResult<List<AssignmentManagerItemDto>>> GetAllAsync(int? classSubjectTeacherId = null)
    {
        var query = _context.Assignments
            .Include(a => a.ClassSubjectTeacher)
                .ThenInclude(c => c.Subject)
            .Include(a => a.ClassSubjectTeacher)
                .ThenInclude(c => c.Class)
                    .ThenInclude(cl => cl.Enrollments)
            .Include(a => a.Submissions)
            .Where(a => !a.IsDeleted)
            .AsQueryable();

        if (classSubjectTeacherId.HasValue)
            query = query.Where(a => a.ClassSubjectTeacherId == classSubjectTeacherId.Value);

        var assignments = await query.ToListAsync();

        var items = assignments.Select(a =>
        {
            var totalStudents = a.ClassSubjectTeacher?.Class?.Enrollments
                .Count(e => !e.IsDeleted && e.AcademicYearId == a.ClassSubjectTeacher.AcademicYearId) ?? 0;
            var submitted = a.Submissions.Count(s => !s.IsDeleted);

            return new AssignmentManagerItemDto
            {
                Id = a.Id,
                Title = a.Title,
                Subject = a.ClassSubjectTeacher?.Subject?.Name ?? "",
                Class = a.ClassSubjectTeacher?.Class?.Name ?? "",
                Deadline = a.DueDate?.ToString("yyyy-MM-ddTHH:mm") ?? "",
                Submitted = submitted,
                Total = totalStudents,
                Status = GetStatus(a)
            };
        }).ToList();

        return OperationResult<List<AssignmentManagerItemDto>>.Success(items);
    }

    public async Task<OperationResult<AssignmentManagerDetailDto>> GetByIdAsync(int id)
    {
        var assignment = await _context.Assignments
            .Include(a => a.ClassSubjectTeacher)
                .ThenInclude(c => c.Subject)
            .Include(a => a.ClassSubjectTeacher)
                .ThenInclude(c => c.Class)
                    .ThenInclude(cl => cl.Enrollments)
            .Include(a => a.Submissions)
            .Include(a => a.Questions.Where(q => !q.IsDeleted))
                .ThenInclude(q => q.Options.Where(o => !o.IsDeleted))
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (assignment is null)
            return OperationResult<AssignmentManagerDetailDto>.Failure("الواجب غير موجود", 404);

        var totalStudents = assignment.ClassSubjectTeacher?.Class?.Enrollments
            .Count(e => !e.IsDeleted && e.AcademicYearId == assignment.ClassSubjectTeacher.AcademicYearId) ?? 0;
        var submitted = assignment.Submissions.Count(s => !s.IsDeleted);

        var dto = new AssignmentManagerDetailDto
        {
            Id = assignment.Id,
            Title = assignment.Title,
            Subject = assignment.ClassSubjectTeacher?.Subject?.Name ?? "",
            Class = assignment.ClassSubjectTeacher?.Class?.Name ?? "",
            Deadline = assignment.DueDate?.ToString("yyyy-MM-ddTHH:mm") ?? "",
            Submitted = submitted,
            Total = totalStudents,
            Status = GetStatus(assignment),
            Questions = assignment.Questions
                .Where(q => !q.IsDeleted)
                .OrderBy(q => q.DisplayOrder)
                .Select(q => new AssignmentManagerQuestionDto
                {
                    Id = q.Id,
                    Type = MapType(q.QuestionType),
                    Text = q.QuestionText,
                    Options = q.Options
                        .Where(o => !o.IsDeleted)
                        .OrderBy(o => o.DisplayOrder)
                        .Select(o => o.OptionText)
                        .ToList(),
                    CorrectAnswer = q.CorrectAnswer ?? ""
                }).ToList()
        };

        return OperationResult<AssignmentManagerDetailDto>.Success(dto);
    }

    public async Task<OperationResult<AssignmentManagerItemDto>> CreateAsync(CreateAssignmentManagerDto dto, int teacherId)
    {
        var academicYear = await _unitOfWork.AcademicYears
            .FindAsync(y => y.IsCurrent && !y.IsDeleted);
        var year = academicYear.FirstOrDefault();
        if (year is null)
            return OperationResult<AssignmentManagerItemDto>.Failure("لا يوجد عام دراسي نشط", 400);

        var csts = await _unitOfWork.ClassSubjectTeachers
            .FindAsync(c => c.ClassId == dto.ClassId && c.SubjectId == dto.SubjectId
                && c.AcademicYearId == year.Id && !c.IsDeleted);
        var cst = csts.FirstOrDefault();
        if (cst is null)
            return OperationResult<AssignmentManagerItemDto>.Failure("لم يتم العثور على رابط الفصل-المادة-المدرس", 400);

        DateTime? dueDate = null;
        if (DateTime.TryParse(dto.Deadline, out var parsed))
            dueDate = parsed;

        var assignment = new Assignment
        {
            ClassSubjectTeacherId = cst.Id,
            Title = dto.Title,
            DueDate = dueDate,
            MaxScore = dto.Questions.Sum(q => q.Type == "essay" ? 10 : 5),
            IsAutoGraded = true,
            IsPublished = true,
            Category = EvaluationCategory.Academic,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Assignments.AddAsync(assignment);
        await _unitOfWork.SaveChangesAsync();

        int displayOrder = 1;
        foreach (var qDto in dto.Questions)
        {
            var question = new AssignmentQuestion
            {
                AssignmentId = assignment.Id,
                QuestionText = qDto.Text,
                QuestionType = MapTypeBack(qDto.Type),
                CorrectAnswer = qDto.CorrectAnswer,
                DisplayOrder = displayOrder++,
                Points = qDto.Type == "essay" ? 10 : 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.AssignmentQuestions.AddAsync(question);
            await _unitOfWork.SaveChangesAsync();

            if (qDto.Type == "mcq" || qDto.Type == "true-false")
            {
                int optOrder = 1;
                foreach (var optText in qDto.Options)
                {
                    var option = new AssignmentQuestionOption
                    {
                        QuestionId = question.Id,
                        OptionText = optText,
                        IsCorrect = optText == qDto.CorrectAnswer,
                        DisplayOrder = optOrder++,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.AssignmentQuestionOptions.AddAsync(option);
                }
                await _unitOfWork.SaveChangesAsync();
            }
        }

        return OperationResult<AssignmentManagerItemDto>.Success(new AssignmentManagerItemDto
        {
            Id = assignment.Id,
            Title = assignment.Title,
            Subject = cst.Subject?.Name ?? "",
            Class = cst.Class?.Name ?? "",
            Deadline = dueDate?.ToString("yyyy-MM-ddTHH:mm") ?? "",
            Submitted = 0,
            Total = cst.Class?.Enrollments
                .Count(e => !e.IsDeleted && e.AcademicYearId == year.Id) ?? 0,
            Status = "active"
        });
    }

    public async Task<OperationResult> UpdateAsync(int id, UpdateAssignmentManagerDto dto)
    {
        var assignment = await _unitOfWork.Assignments.GetWithQuestionsAsync(id);
        if (assignment is null || assignment.IsDeleted)
            return OperationResult.Failure("الواجب غير موجود", 404);

        DateTime? dueDate = null;
        if (DateTime.TryParse(dto.Deadline, out var parsed))
            dueDate = parsed;

        assignment.Title = dto.Title;
        assignment.DueDate = dueDate;
        assignment.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Assignments.Update(assignment);

        var existingQuestions = assignment.Questions.Where(q => !q.IsDeleted).ToList();
        foreach (var eq in existingQuestions)
        {
            eq.IsDeleted = true;
            eq.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.AssignmentQuestions.Update(eq);
        }

        int displayOrder = 1;
        foreach (var qDto in dto.Questions)
        {
            var question = new AssignmentQuestion
            {
                AssignmentId = id,
                QuestionText = qDto.Text,
                QuestionType = MapTypeBack(qDto.Type),
                CorrectAnswer = qDto.CorrectAnswer,
                DisplayOrder = displayOrder++,
                Points = qDto.Type == "essay" ? 10 : 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.AssignmentQuestions.AddAsync(question);
            await _unitOfWork.SaveChangesAsync();

            if (qDto.Type == "mcq" || qDto.Type == "true-false")
            {
                int optOrder = 1;
                foreach (var optText in qDto.Options)
                {
                    var option = new AssignmentQuestionOption
                    {
                        QuestionId = question.Id,
                        OptionText = optText,
                        IsCorrect = optText == qDto.CorrectAnswer,
                        DisplayOrder = optOrder++,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.AssignmentQuestionOptions.AddAsync(option);
                }
                await _unitOfWork.SaveChangesAsync();
            }
        }

        await _unitOfWork.SaveChangesAsync();
        return OperationResult.Success("تم تحديث الواجب بنجاح");
    }

    public async Task<OperationResult> DeleteAsync(int id)
    {
        var assignment = await _unitOfWork.Assignments.GetByIdAsync(id);
        if (assignment is null || assignment.IsDeleted)
            return OperationResult.Failure("الواجب غير موجود", 404);

        assignment.IsDeleted = true;
        assignment.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Assignments.Update(assignment);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("تم حذف الواجب بنجاح");
    }

    public async Task<OperationResult<AssignmentManagerStatsDto>> GetStatsAsync(int? classSubjectTeacherId = null)
    {
        var items = await GetAllAsync(classSubjectTeacherId);
        if (!items.IsSuccess || items.Data is null)
            return OperationResult<AssignmentManagerStatsDto>.Success(new AssignmentManagerStatsDto());

        var list = items.Data;
        return OperationResult<AssignmentManagerStatsDto>.Success(new AssignmentManagerStatsDto
        {
            Total = list.Count,
            Active = list.Count(a => a.Status == "active"),
            AvgDelivery = list.Count > 0
                ? Math.Round(list.Average(a => a.Total > 0 ? (double)a.Submitted / a.Total * 100 : 0), 1)
                : 0,
            Overdue = list.Count(a => a.Status == "active" && a.Submitted < a.Total)
        });
    }

    private static string GetStatus(Assignment a)
    {
        if (!a.IsPublished) return "draft";
        if (a.DueDate.HasValue && a.DueDate.Value < DateTime.UtcNow) return "closed";
        return "active";
    }

    private static string MapType(QuestionType type) => type switch
    {
        QuestionType.MultipleChoice => "mcq",
        QuestionType.TrueFalse => "true-false",
        QuestionType.Essay => "essay",
        _ => "mcq"
    };

    private static QuestionType MapTypeBack(string type) => type switch
    {
        "mcq" => QuestionType.MultipleChoice,
        "true-false" => QuestionType.TrueFalse,
        "essay" => QuestionType.Essay,
        _ => QuestionType.MultipleChoice
    };
}
