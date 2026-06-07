using Project.Domain.Entities;
using Project.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Core;

public interface IStudentEnrollmentRepository : IRepository<StudentEnrollment>
{
    Task<StudentEnrollment?>            GetActiveByStudentAndYearAsync(int studentId, int academicYearId, CancellationToken ct = default);
    Task<IReadOnlyList<StudentEnrollment>> GetByClassAndYearAsync(int classId, int academicYearId, CancellationToken ct = default);
    Task<IReadOnlyList<StudentEnrollment>> GetActiveByClassAsync(int classId, int academicYearId, CancellationToken ct = default);
    Task<IReadOnlyList<StudentEnrollment>> GetHistoryByStudentAsync(int studentId, CancellationToken ct = default);
    Task<IReadOnlyList<StudentEnrollment>> GetTransfersHistoryAsync(int academicYearId, CancellationToken ct = default);

    Task<bool> IsEnrolledAsync(int studentId, int classId, int academicYearId, CancellationToken ct = default);
    Task<bool> HasActiveEnrollmentAsync(int studentId, int academicYearId, CancellationToken ct = default);

    Task<StudentEnrollment?>               GetWithStudentAsync(int enrollmentId, CancellationToken ct = default);
    Task<IReadOnlyList<StudentEnrollment>> GetByClassWithStudentAsync(int classId, int academicYearId, CancellationToken ct = default);

    Task<int> GetActiveCountByClassAsync(int classId, int academicYearId, CancellationToken ct = default);
}



