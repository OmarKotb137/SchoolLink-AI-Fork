using Common.Results;
using Microsoft.Extensions.Logging;
using Project.BLL.DTOs;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

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

    public async Task<OperationResult<UnitDto>> CreateUnitAsync(int subjectId, string name, int displayOrder, List<CreateLessonDto>? lessons = null, AcademicTerm? term = null)
    {
        return await CreateUnitAsync(subjectId, new CreateUnitDto
        {
            Name = name,
            DisplayOrder = displayOrder,
            Lessons = lessons,
            Term = term
        });
    }

    public async Task<OperationResult<UnitDto>> CreateUnitAsync(int subjectId, CreateUnitDto dto)
    {
        var subject = await _unitOfWork.Subjects.GetByIdAsync(subjectId);
        if (subject == null || subject.IsDeleted)
            return OperationResult<UnitDto>.Failure("المادة غير موجودة", 404);

        if (dto.GradeLevelId > 0)
        {
            var gradeLevel = await _unitOfWork.GradeLevels.GetByIdAsync(dto.GradeLevelId);
            if (gradeLevel == null || gradeLevel.IsDeleted)
                return OperationResult<UnitDto>.Failure("الصف الدراسي غير موجود", 404);
        }

        var unit = new Unit
        {
            SubjectId = subjectId,
            GradeLevelId = dto.GradeLevelId,
            Name = dto.Name,
            Content = dto.Content,
            DisplayOrder = dto.DisplayOrder,
            PageStart = dto.PageStart,
            PageEnd = dto.PageEnd,
            Term = dto.Term
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

    public async Task<OperationResult<List<UnitDto>>> GetUnitsBySubjectAsync(int subjectId, AcademicTerm? term = null)
    {
        var subject = await _unitOfWork.Subjects.GetByIdAsync(subjectId);
        if (subject == null || subject.IsDeleted)
            return OperationResult<List<UnitDto>>.Failure("المادة غير موجودة", 404);

        var units = await _unitOfWork.Units.FindAsync(u => u.SubjectId == subjectId && !u.IsDeleted);
        if (term.HasValue)
            units = units.Where(u => u.Term == term.Value).ToList();
        var ordered = units.OrderBy(u => u.DisplayOrder).ToList();

        return OperationResult<List<UnitDto>>.Success(ordered.Select(Map).ToList());
    }

    public async Task<OperationResult<List<UnitDto>>> GetUnitsWithLessonsBySubjectAsync(int subjectId, AcademicTerm? term = null)
    {
        var subject = await _unitOfWork.Subjects.GetByIdAsync(subjectId);
        if (subject == null || subject.IsDeleted)
            return OperationResult<List<UnitDto>>.Failure("المادة غير موجودة", 404);

        var units = await _unitOfWork.Units.FindAsync(u => u.SubjectId == subjectId && !u.IsDeleted);
        if (term.HasValue)
            units = units.Where(u => u.Term == term.Value).ToList();
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

    public async Task<OperationResult<List<UnitDto>>> GetUnitsByGradeLevelAndSubjectAsync(int gradeLevelId, int subjectId, AcademicTerm? term = null)
    {
        var subject = await _unitOfWork.Subjects.GetByIdAsync(subjectId);
        if (subject == null || subject.IsDeleted)
            return OperationResult<List<UnitDto>>.Failure("المادة غير موجودة", 404);

        var gradeLevel = await _unitOfWork.GradeLevels.GetByIdAsync(gradeLevelId);
        if (gradeLevel == null || gradeLevel.IsDeleted)
            return OperationResult<List<UnitDto>>.Failure("الصف الدراسي غير موجود", 404);

        var units = await _unitOfWork.Units.FindAsync(u => u.SubjectId == subjectId && u.GradeLevelId == gradeLevelId && !u.IsDeleted);
        if (term.HasValue)
            units = units.Where(u => u.Term == term.Value).ToList();
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

    public async Task<OperationResult<List<SubjectWithStructureDto>>> GetParsedSubjectsWithStructureAsync(AcademicTerm? term = null)
    {
        try
        {
            var subjects = await _unitOfWork.Subjects.GetAllAsync();
            var allGradeLevels = await _unitOfWork.GradeLevels.GetAllAsync();
            var allClasses = await _unitOfWork.Classes.GetAllAsync();
            var allAssignments = await _unitOfWork.ClassSubjectTeachers.GetAllAsync();
            var result = new List<SubjectWithStructureDto>();

            // Build subject -> grade level lookup (first assignment's grade level)
            var subjectGradeLevel = new Dictionary<int, (string Name, int Id)>();
            foreach (var assignment in allAssignments.Where(a => !a.IsDeleted))
            {
                var cls = allClasses.FirstOrDefault(c => c.Id == assignment.ClassId && !c.IsDeleted);
                if (cls is null) continue;
                var gl = allGradeLevels.FirstOrDefault(g => g.Id == cls.GradeLevelId);
                if (gl is null) continue;
                if (!subjectGradeLevel.ContainsKey(assignment.SubjectId))
                    subjectGradeLevel[assignment.SubjectId] = (gl.Name, gl.Id);
            }

            foreach (var subject in subjects.Where(s => !s.IsDeleted))
            {
                var units = await _unitOfWork.Units.FindAsync(u => u.SubjectId == subject.Id && !u.IsDeleted);
                if (term.HasValue)
                    units = units.Where(u => u.Term == term.Value).ToList();
                if (units.Count == 0) continue;

                var lessonCount = 0;
                foreach (var unit in units)
                {
                    var lessons = await _unitOfWork.Lessons.FindAsync(l => l.UnitId == unit.Id && !l.IsDeleted);
                    lessonCount += lessons.Count;
                }

                subjectGradeLevel.TryGetValue(subject.Id, out var glInfo);

                result.Add(new SubjectWithStructureDto
                {
                    Id = subject.Id,
                    Name = subject.Name,
                    GradeLevelId = glInfo.Id,
                    GradeLevelName = glInfo.Name,
                    UnitCount = units.Count,
                    LessonCount = lessonCount,
                    Term = units.FirstOrDefault()?.Term
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

    public async Task<OperationResult<UnitDto>> UpdateUnitAsync(int id, string name, string? content = null, int? pageStart = null, int? pageEnd = null, AcademicTerm? term = null)
    {
        var unit = await _unitOfWork.Units.GetByIdAsync(id);
        if (unit is null || unit.IsDeleted)
            return OperationResult<UnitDto>.Failure("الوحدة غير موجودة", 404);

        unit.Name = name;
        if (content is not null) unit.Content = content;
        if (pageStart is not null) unit.PageStart = pageStart;
        if (pageEnd is not null) unit.PageEnd = pageEnd;
        if (term is not null) unit.Term = term;
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

    public async Task<OperationResult> DeleteLessonAsync(int lessonId)
    {
        var lesson = await _unitOfWork.Lessons.GetByIdAsync(lessonId);
        if (lesson == null || lesson.IsDeleted)
            return OperationResult.Failure("الدرس غير موجود", 404);

        lesson.IsDeleted = true;
        lesson.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Deleted lesson {LessonId}", lessonId);

        return OperationResult.Success("تم حذف الدرس بنجاح");
    }

    private static UnitDto Map(Unit u) => new()
    {
        Id = u.Id,
        SubjectId = u.SubjectId,
        GradeLevelId = u.GradeLevelId,
        Name = u.Name,
        Content = u.Content,
        DisplayOrder = u.DisplayOrder,
        PageStart = u.PageStart,
        PageEnd = u.PageEnd,
        SubjectName = u.Subject?.Name,
        GradeLevelName = u.GradeLevel?.Name,
        Term = u.Term
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
