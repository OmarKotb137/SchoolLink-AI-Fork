using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories;
using Project.DAL.Context;
using System.Linq.Expressions;

namespace Project.DAL.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet   = context.Set<T>();
    }


    public virtual async Task<T?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(
            e => EF.Property<int>(e, "Id") == id,
            ct);

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
        => await _dbSet.ToListAsync(ct);

    public virtual async Task<IReadOnlyList<T>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken ct = default)
        => await ApplyPaging(_dbSet, pageNumber, pageSize).ToListAsync(ct);

    public virtual async Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default)
        => await _dbSet.Where(predicate).ToListAsync(ct);

    public virtual async Task<IReadOnlyList<T>> FindPagedAsync(
        Expression<Func<T, bool>> predicate,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default)
        => await ApplyPaging(_dbSet.Where(predicate), pageNumber, pageSize).ToListAsync(ct);

    public virtual async Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(predicate, ct);

    public virtual async Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default)
        => await _dbSet.AnyAsync(predicate, ct);

    public virtual async Task<int> CountAsync(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken ct = default)
        => predicate is null
            ? await _dbSet.CountAsync(ct)
            : await _dbSet.CountAsync(predicate, ct);

    public virtual async Task<bool> ExistsAsync(int id, CancellationToken ct = default)
        => await _dbSet.AnyAsync(
            e => EF.Property<int>(e, "Id") == id,
            ct);


    public virtual async Task AddAsync(T entity, CancellationToken ct = default)
        => await _dbSet.AddAsync(entity, ct);

    public virtual async Task AddRangeAsync(
        IEnumerable<T> entities,
        CancellationToken ct = default)
        => await _dbSet.AddRangeAsync(entities, ct);

    public virtual void Update(T entity)
    {
        _dbSet.Attach(entity);
        _context.Entry(entity).State = EntityState.Modified;
    }

    public virtual void UpdateRange(IEnumerable<T> entities)
    {
        foreach (var entity in entities)
            Update(entity);
    }

    public virtual void SoftDelete(T entity)
    {
        var entry = _context.Entry(entity);
        entry.Property("IsDeleted").CurrentValue = true;
        entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
        entry.State = EntityState.Modified;
    }

    public virtual void SoftDeleteRange(IEnumerable<T> entities)
    {
        foreach (var entity in entities)
            SoftDelete(entity);
    }

    private static IQueryable<T> ApplyPaging(IQueryable<T> query, int pageNumber, int pageSize)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Max(pageSize, 1);

        return query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);
    }
}

