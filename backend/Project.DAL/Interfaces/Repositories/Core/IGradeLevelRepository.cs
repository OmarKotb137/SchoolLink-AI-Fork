using Project.Domain.Entities;
using Project.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Core;

public interface IGradeLevelRepository : IRepository<GradeLevel>
{
    Task<GradeLevel?>               GetByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<GradeLevel>> GetByStageAsync(string stage, CancellationToken ct = default);
    Task<IReadOnlyList<GradeLevel>> GetAllOrderedAsync(CancellationToken ct = default);
    Task<IReadOnlyList<string>>     GetDistinctStagesAsync(CancellationToken ct = default);
}



