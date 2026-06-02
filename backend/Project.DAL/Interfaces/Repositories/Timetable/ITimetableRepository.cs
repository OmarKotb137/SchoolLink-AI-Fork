using Project.Domain.Entities;
using Project.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Timetable;

public interface ITimetableRepository : IRepository<Project.Domain.Entities.Timetable>
{
    Task<Project.Domain.Entities.Timetable?> GetActiveByClassAndYearAsync(int classId, int academicYearId, CancellationToken ct = default);
    Task<bool>       HasActiveTimetableAsync(int classId, int academicYearId, CancellationToken ct = default);

    Task<IReadOnlyList<Project.Domain.Entities.Timetable>> GetByClassAndYearAsync(int classId, int academicYearId, CancellationToken ct = default);

    Task<Project.Domain.Entities.Timetable?> GetWithSlotsAsync(int timetableId, CancellationToken ct = default);

    Task<IReadOnlyList<Project.Domain.Entities.Timetable>> GetByClassAndYearWithDetailsAsync(int classId, int academicYearId, CancellationToken ct = default);

    Task DeactivateByClassAndYearAsync(int classId, int academicYearId, CancellationToken ct = default);
    /// يجيب Timetable مع:
    ///   - Class navigation property (لـ ClassName في الـ DTO)
    ///   - كل الـ slots غير المحذوفة (lessons + breaks) مُرتَّبة بـ DayOfWeek ثم PeriodNumber
    ///   - كل slot فيها CST → Subject + Teacher
    /// يُستخدم في: CreateTimetableAsync (بعد save)، GetByClassAsync، GetByStudentAsync.
    /// (GetWithSlotsAsync الموجودة مش بتجيب Class وبتفلتر الـ IsBreak=true)
    /// </summary>
    Task<Project.Domain.Entities.Timetable?> GetWithClassAndAllSlotsAsync(
        int timetableId,
        CancellationToken ct = default);
}


