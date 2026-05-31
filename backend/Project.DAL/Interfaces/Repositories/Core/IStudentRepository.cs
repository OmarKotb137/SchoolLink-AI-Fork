using SchoolLink.Domain.Entities;
using SchoolLink.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Core;

public interface IStudentRepository : IRepository<Student>
{
    Task<Student?> GetByNationalIdAsync(string nationalId, CancellationToken ct = default);
    Task<Student?> GetByUserIdAsync(int userId, CancellationToken ct = default);
    Task<bool>     ExistsByNationalIdAsync(string nationalId, CancellationToken ct = default);

    Task<IReadOnlyList<Student>> SearchByNameAsync(string query, CancellationToken ct = default);
    Task<IReadOnlyList<Student>> GetActiveStudentsAsync(CancellationToken ct = default);

    Task<Student?>               GetWithCurrentEnrollmentAsync(int studentId, int academicYearId, CancellationToken ct = default);
    Task<IReadOnlyList<Student>> GetByClassAsync(int classId, int academicYearId, CancellationToken ct = default);
    Task<IReadOnlyList<Student>> GetWithoutUserAccountAsync(CancellationToken ct = default);
}



