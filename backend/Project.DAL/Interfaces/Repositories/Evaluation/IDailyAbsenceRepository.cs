using Project.Domain.Entities;
using Project.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Evaluation;

public interface IDailyAbsenceRepository : IRepository<DailyAbsence>
{
    Task<IReadOnlyList<DailyAbsence>> GetByEnrollmentIdAsync(int enrollmentId, CancellationToken ct = default);
    Task<IReadOnlyList<DailyAbsence>> GetByEnrollmentAndDateRangeAsync(int enrollmentId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<IReadOnlyList<DailyAbsence>> GetByEnrollmentAndMonthAsync(int enrollmentId, int month, int year, CancellationToken ct = default);
    Task<IReadOnlyList<DateOnly>>     GetAbsenceDatesAsync(int enrollmentId, int? classSubjectTeacherId = null, CancellationToken ct = default);

    Task<IReadOnlyList<DailyAbsence>> GetByClassSubjectTeacherAndDateAsync(int classSubjectTeacherId, DateOnly date, CancellationToken ct = default);
    Task<IReadOnlyList<DailyAbsence>> GetByClassSubjectTeacherAndDateRangeAsync(int classSubjectTeacherId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<IReadOnlyList<DailyAbsence>> GetAbsentStudentsByDateAsync(int classSubjectTeacherId, DateOnly date, CancellationToken ct = default);

    Task<int> GetAbsenceCountAsync(int enrollmentId, int? classSubjectTeacherId = null, DateOnly? from = null, DateOnly? to = null, CancellationToken ct = default);
    Task<int> GetAbsenceCountByMonthAsync(int enrollmentId, int month, int year, CancellationToken ct = default);

    Task<bool> IsAbsentAsync(int enrollmentId, DateOnly date, int? classSubjectTeacherId = null, CancellationToken ct = default);

    Task<IReadOnlyList<int>> GetEnrollmentsWithAbsenceExceedingAsync(int classId, int academicYearId, int threshold, CancellationToken ct = default);

    Task BulkUpsertAsync(IEnumerable<DailyAbsence> absences, CancellationToken ct = default);

    Task<IReadOnlyList<DailyAbsence>> GetByEnrollmentsAndDateRangeAsync(
        List<int> enrollmentIds, DateOnly from, DateOnly to, CancellationToken ct = default);
}



