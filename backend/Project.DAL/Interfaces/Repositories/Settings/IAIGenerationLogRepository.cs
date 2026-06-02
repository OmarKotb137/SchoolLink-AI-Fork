using Project.Domain.Entities;
using Project.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Settings;

public interface IAIGenerationLogRepository : IRepository<AIGenerationLog>
{
    Task<IReadOnlyList<AIGenerationLog>> GetByUserIdAsync(int userId, CancellationToken ct = default);
    Task<IReadOnlyList<AIGenerationLog>> GetByOperationTypeAsync(string operationType, CancellationToken ct = default);
    Task<IReadOnlyList<AIGenerationLog>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<AIGenerationLog>> GetByUserAndOperationAsync(int userId, string operationType, CancellationToken ct = default);
    Task<IReadOnlyList<AIGenerationLog>> GetFailedAsync(CancellationToken ct = default);

    Task<int>    GetTotalTokensUsedAsync(int? userId = null, string? operationType = null, CancellationToken ct = default);
    Task<int>    GetTotalTokensUsedByDateRangeAsync(DateTime from, DateTime to, string? operationType = null, CancellationToken ct = default);
    Task<Dictionary<string, int>> GetTokenUsageByOperationTypeAsync(DateTime from, DateTime to, CancellationToken ct = default);

    Task<double> GetAverageLatencyAsync(string operationType, CancellationToken ct = default);
    Task<double> GetSuccessRateAsync(string? operationType = null, CancellationToken ct = default);
    Task<int>    GetCallCountAsync(string? operationType = null, DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
}



