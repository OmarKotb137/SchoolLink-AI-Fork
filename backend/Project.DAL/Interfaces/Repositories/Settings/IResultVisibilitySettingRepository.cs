using Project.Domain.Entities;
using Project.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Settings;

public interface IResultVisibilitySettingRepository : IRepository<ResultVisibilitySetting>
{
    Task<ResultVisibilitySetting?> GetByAcademicYearAndTermAsync(int academicYearId, AcademicTerm term, CancellationToken ct = default);
    Task<bool>                     IsVisibleAsync(int academicYearId, AcademicTerm term, CancellationToken ct = default);
    Task<bool>                     ExistsByYearAndTermAsync(int academicYearId, AcademicTerm term, CancellationToken ct = default);

    Task<IReadOnlyList<ResultVisibilitySetting>> GetByAcademicYearIdAsync(int academicYearId, CancellationToken ct = default);

    Task UpsertAsync(ResultVisibilitySetting setting, CancellationToken ct = default);
}



