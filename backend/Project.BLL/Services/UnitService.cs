using Common.Results;
using Microsoft.Extensions.Logging;
using Project.BLL.DTOs;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;

namespace Project.BLL.Services;

public class UnitService : IUnitService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UnitService> _logger;

    public UnitService(IUnitOfWork unitOfWork, ILogger<UnitService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<OperationResult<UnitDto>> CreateUnitAsync(int subjectId, string name, int displayOrder, List<CreateLessonDto>? lessons = null)
    {
        return await CreateUnitAsync(subjectId, new CreateUnitDto
        {
            Name = name,
            DisplayOrder = displayOrder,
            Lessons = lessons
        });
    }

    public async Task<OperationResult<UnitDto>> CreateUnitAsync(int subjectId, CreateUnitDto dto)
    {
        var subject = await _unitOfWork.Subjects.GetByIdAsync(subjectId);
        if (subject == null || subject.IsDeleted)
            return OperationResult<UnitDto>.Failure("المادة غير موجودة", 404);

        var unit = new Unit
        {
            SubjectId = subjectId,
            Name = dto.Name,
            Content = dto.Content,
            DisplayOrder = dto.DisplayOrder,
            PageStart = dto.PageStart,
            PageEnd = dto.PageEnd
        };

        if (dto.Lessons?.Count > 0)
        {
            unit.Lessons = dto.Lessons.Select((l, i) => new Lesson
            {
                Title = l.Title,
                Content = l.Content ?? "",
                DisplayOrder = l.DisplayOrder > 0 ? l.DisplayOrder : i + 1,
                PageStart = l.PageStart,
                PageEnd = l.PageEnd
            }).ToList();
        }

        await _unitOfWork.Units.AddAsync(unit);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created unit {UnitName} with {LessonCount} lessons", dto.Name, dto.Lessons?.Count ?? 0);

        return OperationResult<UnitDto>.Success(Map(unit));
    }

    public async Task<OperationResult<List<UnitDto>>> GetUnitsBySubjectAsync(int subjectId)
    {
        var subject = await _unitOfWork.Subjects.GetByIdAsync(subjectId);
        if (subject == null || subject.IsDeleted)
            return OperationResult<List<UnitDto>>.Failure("المادة غير موجودة", 404);

        var units = await _unitOfWork.Units.FindAsync(u => u.SubjectId == subjectId && !u.IsDeleted);
        var ordered = units.OrderBy(u => u.DisplayOrder).ToList();

        return OperationResult<List<UnitDto>>.Success(ordered.Select(Map).ToList());
    }

    public async Task<OperationResult<List<LessonDto>>> GetLessonsByUnitAsync(int unitId)
    {
        var unit = await _unitOfWork.Units.GetByIdAsync(unitId);
        if (unit == null || unit.IsDeleted)
            return OperationResult<List<LessonDto>>.Failure("الوحدة غير موجودة", 404);

        var lessons = await _unitOfWork.Lessons.FindAsync(l => l.UnitId == unitId && !l.IsDeleted);
        var ordered = lessons.OrderBy(l => l.DisplayOrder).ToList();

        return OperationResult<List<LessonDto>>.Success(ordered.Select(MapLesson).ToList());
    }

    public async Task<OperationResult> DeleteUnitAsync(int unitId)
    {
        var unit = await _unitOfWork.Units.GetByIdAsync(unitId);
        if (unit == null || unit.IsDeleted)
            return OperationResult.Failure("الوحدة غير موجودة", 404);

        var lessons = await _unitOfWork.Lessons.FindAsync(l => l.UnitId == unitId && !l.IsDeleted);
        foreach (var lesson in lessons)
        {
            lesson.IsDeleted = true;
            lesson.UpdatedAt = DateTime.UtcNow;
        }

        unit.IsDeleted = true;
        unit.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Deleted unit {UnitId} and {LessonCount} lessons", unitId, lessons.Count);

        return OperationResult.Success("تم حذف الوحدة بنجاح");
    }

    private static UnitDto Map(Unit u) => new()
    {
        Id = u.Id,
        SubjectId = u.SubjectId,
        Name = u.Name,
        Content = u.Content,
        DisplayOrder = u.DisplayOrder,
        PageStart = u.PageStart,
        PageEnd = u.PageEnd,
        SubjectName = u.Subject?.Name
    };

    private static LessonDto MapLesson(Lesson l) => new()
    {
        Id = l.Id,
        UnitId = l.UnitId,
        Title = l.Title,
        Content = l.Content,
        DisplayOrder = l.DisplayOrder,
        PageStart = l.PageStart,
        PageEnd = l.PageEnd
    };
}
