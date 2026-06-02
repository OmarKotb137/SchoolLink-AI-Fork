using Project.Domain.Entities;
using Project.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Core;

public interface IAcademicYearRepository : IRepository<AcademicYear>
{
    Task<AcademicYear?>            GetCurrentAsync(CancellationToken ct = default);
    Task<AcademicYear?>            GetByNameAsync(string name, CancellationToken ct = default);
    Task<bool>                     ExistsByNameAsync(string name, CancellationToken ct = default);
    Task<bool>                     HasCurrentYearAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AcademicYear>> GetAllOrderedByStartDateAsync(CancellationToken ct = default);
}



