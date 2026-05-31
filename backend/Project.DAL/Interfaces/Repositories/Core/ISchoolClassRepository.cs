using SchoolLink.Domain.Entities;
using SchoolLink.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Core;

public interface ISchoolClassRepository : IRepository<SchoolClass>
{
    Task<IReadOnlyList<SchoolClass>> GetByGradeLevelAndYearAsync(int gradeLevelId, int academicYearId, CancellationToken ct = default);
    Task<IReadOnlyList<SchoolClass>> GetByAcademicYearAsync(int academicYearId, CancellationToken ct = default);

    Task<SchoolClass?> GetByNameGradeLevelAndYearAsync(string name, int gradeLevelId, int academicYearId, CancellationToken ct = default);
    Task<bool>         ExistsByNameGradeLevelAndYearAsync(string name, int gradeLevelId, int academicYearId, CancellationToken ct = default);

    Task<int>                        GetEnrollmentCountAsync(int classId, int academicYearId, CancellationToken ct = default);
    Task<IReadOnlyList<SchoolClass>> GetWithEnrollmentCountAsync(int academicYearId, CancellationToken ct = default);
}



