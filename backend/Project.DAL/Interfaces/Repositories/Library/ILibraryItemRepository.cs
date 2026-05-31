using SchoolLink.Domain.Entities;
using SchoolLink.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Library;

public interface ILibraryItemRepository : IRepository<LibraryItem>
{
    Task<IReadOnlyList<LibraryItem>> GetBySubjectIdAsync(int subjectId, CancellationToken ct = default);
    Task<IReadOnlyList<LibraryItem>> GetByGradeLevelIdAsync(int gradeLevelId, CancellationToken ct = default);
    Task<IReadOnlyList<LibraryItem>> GetByAcademicYearIdAsync(int academicYearId, CancellationToken ct = default);
    Task<IReadOnlyList<LibraryItem>> GetBySubjectAndGradeLevelAsync(int subjectId, int gradeLevelId, CancellationToken ct = default);
    Task<IReadOnlyList<LibraryItem>> GetBySubjectGradeLevelAndTypeAsync(int subjectId, int gradeLevelId, LibraryItemType type, CancellationToken ct = default);
    Task<IReadOnlyList<LibraryItem>> GetByTypeAsync(LibraryItemType itemType, CancellationToken ct = default);
    Task<IReadOnlyList<LibraryItem>> GetActiveAsync(CancellationToken ct = default);

    Task<IReadOnlyList<LibraryItem>> SearchByTitleAsync(string query, CancellationToken ct = default);

    Task<IReadOnlyList<LibraryItem>> GetByUploaderIdAsync(int userId, CancellationToken ct = default);

    Task<long> GetTotalSizeBytesAsync(CancellationToken ct = default);
    Task<long> GetSizeByGradeLevelAsync(int gradeLevelId, CancellationToken ct = default);
}



