using SchoolLink.Domain.Entities;
using SchoolLink.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Core;

public interface IParentStudentRepository : IRepository<ParentStudent>
{
    Task<IReadOnlyList<ParentStudent>> GetByParentIdAsync(int parentId, CancellationToken ct = default);
    Task<IReadOnlyList<ParentStudent>> GetByStudentIdAsync(int studentId, CancellationToken ct = default);
    Task<ParentStudent?>               GetByParentAndStudentAsync(int parentId, int studentId, CancellationToken ct = default);
    Task<bool>                         ExistsByParentAndStudentAsync(int parentId, int studentId, CancellationToken ct = default);

    Task<IReadOnlyList<ParentStudent>> GetWithStudentDetailsByParentAsync(int parentId, CancellationToken ct = default);
    Task<IReadOnlyList<ParentStudent>> GetWithParentDetailsByStudentAsync(int studentId, CancellationToken ct = default);
}



