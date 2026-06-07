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

    public async Task<OperationResult<List<UnitDto>>> GetUnitsWithLessonsBySubjectAsync(int subjectId)
    {
        var subject = await _unitOfWork.Subjects.GetByIdAsync(subjectId);
        if (subject == null || subject.IsDeleted)
            return OperationResult<List<UnitDto>>.Failure("المادة غير موجودة", 404);

        var units = await _unitOfWork.Units.FindAsync(u => u.SubjectId == subjectId && !u.IsDeleted);
        var ordered = units.OrderBy(u => u.DisplayOrder).ToList();
        var dtos = new List<UnitDto>();

        foreach (var unit in ordered)
        {
            var dto = Map(unit);
            var lessons = await _unitOfWork.Lessons.FindAsync(l => l.UnitId == unit.Id && !l.IsDeleted);
            dto.Lessons = lessons.OrderBy(l => l.DisplayOrder).Select(MapLesson).ToList();
            dtos.Add(dto);
        }

        return OperationResult<List<UnitDto>>.Success(dtos);
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

    public async Task<OperationResult<List<SubjectWithStructureDto>>> GetParsedSubjectsWithStructureAsync()
    {
        try
        {
            var subjects = await _unitOfWork.Subjects.GetAllAsync();
            var allGradeLevels = await _unitOfWork.GradeLevels.GetAllAsync();
            var allClasses = await _unitOfWork.Classes.GetAllAsync();
            var allAssignments = await _unitOfWork.ClassSubjectTeachers.GetAllAsync();
            var result = new List<SubjectWithStructureDto>();

            // Build subject -> grade level lookup
            var subjectGradeLevel = new Dictionary<int, string>();
            foreach (var assignment in allAssignments.Where(a => !a.IsDeleted))
            {
                var cls = allClasses.FirstOrDefault(c => c.Id == assignment.ClassId && !c.IsDeleted);
                if (cls is null) continue;
                var gl = allGradeLevels.FirstOrDefault(g => g.Id == cls.GradeLevelId);
                if (gl is null) continue;
                // Keep only the first grade level found per subject
                if (!subjectGradeLevel.ContainsKey(assignment.SubjectId))
                    subjectGradeLevel[assignment.SubjectId] = gl.Name;
            }

            foreach (var subject in subjects.Where(s => !s.IsDeleted))
            {
                var units = await _unitOfWork.Units.FindAsync(u => u.SubjectId == subject.Id && !u.IsDeleted);
                if (units.Count == 0) continue;

                var lessonCount = 0;
                foreach (var unit in units)
                {
                    var lessons = await _unitOfWork.Lessons.FindAsync(l => l.UnitId == unit.Id && !l.IsDeleted);
                    lessonCount += lessons.Count;
                }

                subjectGradeLevel.TryGetValue(subject.Id, out var gradeLevelName);

                result.Add(new SubjectWithStructureDto
                {
                    Id = subject.Id,
                    Name = subject.Name,
                    GradeLevelName = gradeLevelName,
                    UnitCount = units.Count,
                    LessonCount = lessonCount
                });
            }

            return OperationResult<List<SubjectWithStructureDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetParsedSubjectsWithStructure failed");
            return OperationResult<List<SubjectWithStructureDto>>.Failure($"حدث خطأ: {ex.Message}", 500);
        }
    }

    public async Task<OperationResult<UnitDto>> UpdateUnitAsync(int id, string name, string? content = null, int? pageStart = null, int? pageEnd = null)
    {
        var unit = await _unitOfWork.Units.GetByIdAsync(id);
        if (unit is null || unit.IsDeleted)
            return OperationResult<UnitDto>.Failure("الوحدة غير موجودة", 404);

        unit.Name = name;
        if (content is not null) unit.Content = content;
        if (pageStart is not null) unit.PageStart = pageStart;
        if (pageEnd is not null) unit.PageEnd = pageEnd;
        unit.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Units.Update(unit);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult<UnitDto>.Success(Map(unit), "تم تحديث الوحدة");
    }

    public async Task<OperationResult<LessonDto>> UpdateLessonAsync(int id, string title, string? content = null, int? pageStart = null, int? pageEnd = null)
    {
        var lesson = await _unitOfWork.Lessons.GetByIdAsync(id);
        if (lesson is null || lesson.IsDeleted)
            return OperationResult<LessonDto>.Failure("الدرس غير موجود", 404);

        lesson.Title = title;
        if (content is not null) lesson.Content = content;
        if (pageStart is not null) lesson.PageStart = pageStart;
        if (pageEnd is not null) lesson.PageEnd = pageEnd;
        lesson.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Lessons.Update(lesson);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult<LessonDto>.Success(MapLesson(lesson), "تم تحديث الدرس");
    }

    public async Task<OperationResult<LessonDto>> CreateLessonAsync(int unitId, CreateLessonDto dto)
    {
        var unit = await _unitOfWork.Units.GetByIdAsync(unitId);
        if (unit is null || unit.IsDeleted)
            return OperationResult<LessonDto>.Failure("الوحدة غير موجودة", 404);

        var lesson = new Lesson
        {
            UnitId = unitId,
            Title = dto.Title,
            Content = dto.Content ?? "",
            DisplayOrder = dto.DisplayOrder > 0 ? dto.DisplayOrder : 1,
            PageStart = dto.PageStart,
            PageEnd = dto.PageEnd
        };

        await _unitOfWork.Lessons.AddAsync(lesson);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult<LessonDto>.Success(MapLesson(lesson), "تم إنشاء الدرس");
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
