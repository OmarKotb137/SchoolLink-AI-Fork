using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Settings;
using SchoolLink.Domain.Entities;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Settings;

public class AIGenerationLogRepository : Repository<AIGenerationLog>, IAIGenerationLogRepository
{
    public AIGenerationLogRepository(AppDbContext context) : base(context) { }


    public async Task<IReadOnlyList<AIGenerationLog>> GetByUserIdAsync(
        int userId,
        CancellationToken ct = default)
        => await _context.AIGenerationLogs
            .Where(log => log.UserId == userId)
            .OrderByDescending(log => log.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AIGenerationLog>> GetByOperationTypeAsync(
        string operationType,
        CancellationToken ct = default)
        => await _context.AIGenerationLogs
            .Where(log => log.OperationType == operationType)
            .OrderByDescending(log => log.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AIGenerationLog>> GetByDateRangeAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct = default)
        => await _context.AIGenerationLogs
            .Where(log =>
                log.CreatedAt >= from &&
                log.CreatedAt <= to)
            .OrderByDescending(log => log.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AIGenerationLog>> GetByUserAndOperationAsync(
        int userId,
        string operationType,
        CancellationToken ct = default)
        => await _context.AIGenerationLogs
            .Where(log =>
                log.UserId        == userId        &&
                log.OperationType == operationType)
            .OrderByDescending(log => log.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AIGenerationLog>> GetFailedAsync(
        CancellationToken ct = default)
        => await _context.AIGenerationLogs
            .Where(log => !log.IsSuccess)
            .OrderByDescending(log => log.CreatedAt)
            .ToListAsync(ct);


    public async Task<int> GetTotalTokensUsedAsync(
        int? userId            = null,
        string? operationType  = null,
        CancellationToken ct   = default)
    {
        var query = _context.AIGenerationLogs
            .Where(log => log.TokensUsed.HasValue);

        if (userId.HasValue)
            query = query.Where(log => log.UserId == userId.Value);

        if (operationType is not null)
            query = query.Where(log => log.OperationType == operationType);

        var result = await query.SumAsync(log => (long?)log.TokensUsed, ct);
        return (int)(result ?? 0L);
    }

    public async Task<int> GetTotalTokensUsedByDateRangeAsync(
        DateTime from,
        DateTime to,
        string? operationType = null,
        CancellationToken ct  = default)
    {
        var query = _context.AIGenerationLogs
            .Where(log =>
                log.CreatedAt    >= from &&
                log.CreatedAt    <= to   &&
                log.TokensUsed.HasValue);

        if (operationType is not null)
            query = query.Where(log => log.OperationType == operationType);

        var result = await query.SumAsync(log => (long?)log.TokensUsed, ct);
        return (int)(result ?? 0L);
    }

    public async Task<Dictionary<string, int>> GetTokenUsageByOperationTypeAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct = default)
    {
        var groups = await _context.AIGenerationLogs
            .Where(log =>
                log.CreatedAt    >= from &&
                log.CreatedAt    <= to   &&
                log.TokensUsed.HasValue)
            .GroupBy(log => log.OperationType)
            .Select(g => new
            {
                OperationType = g.Key,
                Total         = g.Sum(l => (long)l.TokensUsed!.Value)
            })
            .ToListAsync(ct);

        return groups.ToDictionary(x => x.OperationType, x => (int)x.Total);
    }


    public async Task<double> GetAverageLatencyAsync(
        string operationType,
        CancellationToken ct = default)
    {
        var result = await _context.AIGenerationLogs
            .Where(log =>
                log.OperationType == operationType &&
                log.LatencyMs.HasValue)
            .Select(log => (double?)log.LatencyMs!.Value)
            .AverageAsync(ct);

        return result ?? 0.0;
    }

    public async Task<double> GetSuccessRateAsync(
        string? operationType = null,
        CancellationToken ct  = default)
    {
        var query = _context.AIGenerationLogs.AsQueryable();

        if (operationType is not null)
            query = query.Where(log => log.OperationType == operationType);

        var total = await query.CountAsync(ct);
        if (total == 0) return 0.0;

        var successful = await query.CountAsync(log => log.IsSuccess, ct);
        return (double)successful / total * 100.0;
    }

    public async Task<int> GetCallCountAsync(
        string? operationType = null,
        DateTime? from        = null,
        DateTime? to          = null,
        CancellationToken ct  = default)
    {
        var query = _context.AIGenerationLogs.AsQueryable();

        if (operationType is not null)
            query = query.Where(log => log.OperationType == operationType);

        if (from.HasValue)
            query = query.Where(log => log.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(log => log.CreatedAt <= to.Value);

        return await query.CountAsync(ct);
    }
}



