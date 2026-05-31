using SchoolLink.Domain.Entities;
using SchoolLink.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Core;

public interface ISubjectRepository : IRepository<Subject>
{
    Task<Subject?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<Subject?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<bool>     ExistsByNameAsync(string name, CancellationToken ct = default);
    Task<bool>     ExistsByCodeAsync(string code, CancellationToken ct = default);
}



