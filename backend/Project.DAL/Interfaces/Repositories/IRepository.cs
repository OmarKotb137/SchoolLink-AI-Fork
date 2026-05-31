using SchoolLink.Domain.Entities;
using SchoolLink.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories;

public interface IRepository<T> where T : class
{
    Task<T?>                   GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<T>>     GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<T>>     FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<T?>                   FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<bool>                 AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<int>                  CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);
    Task<bool>                 ExistsAsync(int id, CancellationToken ct = default);

    Task   AddAsync(T entity, CancellationToken ct = default);
    Task   AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
    void   Update(T entity);
    void   UpdateRange(IEnumerable<T> entities);
    void   SoftDelete(T entity);
    void   SoftDeleteRange(IEnumerable<T> entities);
}



