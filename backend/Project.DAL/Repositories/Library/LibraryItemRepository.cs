using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Library;
using SchoolLink.Domain.Entities;
using SchoolLink.Domain.Enums;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Library;

public class LibraryItemRepository : Repository<LibraryItem>, ILibraryItemRepository
{
    public LibraryItemRepository(AppDbContext context) : base(context) { }


    public async Task<IReadOnlyList<LibraryItem>> GetBySubjectIdAsync(
        int subjectId,
        CancellationToken ct = default)
        => await _context.LibraryItems
            .Where(li => li.SubjectId == subjectId && li.IsActive)
            .Include(li => li.UploadedBy)
            .OrderByDescending(li => li.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LibraryItem>> GetByGradeLevelIdAsync(
        int gradeLevelId,
        CancellationToken ct = default)
        => await _context.LibraryItems
            .Where(li => li.GradeLevelId == gradeLevelId && li.IsActive)
            .Include(li => li.UploadedBy)
            .OrderByDescending(li => li.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LibraryItem>> GetByAcademicYearIdAsync(
        int academicYearId,
        CancellationToken ct = default)
        => await _context.LibraryItems
            .Where(li => li.AcademicYearId == academicYearId && li.IsActive)
            .Include(li => li.UploadedBy)
            .OrderByDescending(li => li.CreatedAt)
            .ToListAsync(ct);


    public async Task<IReadOnlyList<LibraryItem>> GetBySubjectAndGradeLevelAsync(
        int subjectId,
        int gradeLevelId,
        CancellationToken ct = default)
        => await _context.LibraryItems
            .Where(li =>
                li.SubjectId    == subjectId    &&
                li.GradeLevelId == gradeLevelId &&
                li.IsActive)
            .Include(li => li.UploadedBy)
            .OrderByDescending(li => li.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LibraryItem>> GetBySubjectGradeLevelAndTypeAsync(
        int subjectId,
        int gradeLevelId,
        LibraryItemType type,
        CancellationToken ct = default)
        => await _context.LibraryItems
            .Where(li =>
                li.SubjectId    == subjectId    &&
                li.GradeLevelId == gradeLevelId &&
                li.ItemType     == type         &&
                li.IsActive)
            .Include(li => li.UploadedBy)
            .OrderByDescending(li => li.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LibraryItem>> GetByTypeAsync(
        LibraryItemType itemType,
        CancellationToken ct = default)
        => await _context.LibraryItems
            .Where(li => li.ItemType == itemType && li.IsActive)
            .Include(li => li.UploadedBy)
            .OrderByDescending(li => li.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LibraryItem>> GetActiveAsync(
        CancellationToken ct = default)
        => await _context.LibraryItems
            .Where(li => li.IsActive)
            .Include(li => li.UploadedBy)
            .OrderByDescending(li => li.CreatedAt)
            .ToListAsync(ct);


    public async Task<IReadOnlyList<LibraryItem>> SearchByTitleAsync(
        string query,
        CancellationToken ct = default)
        => await _context.LibraryItems
            .Where(li => li.Title.Contains(query) && li.IsActive)
            .Include(li => li.UploadedBy)
            .OrderByDescending(li => li.CreatedAt)
            .ToListAsync(ct);


    public async Task<IReadOnlyList<LibraryItem>> GetByUploaderIdAsync(
        int userId,
        CancellationToken ct = default)
        => await _context.LibraryItems
            .Where(li => li.UploadedById == userId)
            .OrderByDescending(li => li.CreatedAt)
            .ToListAsync(ct);


    public async Task<long> GetTotalSizeBytesAsync(
        CancellationToken ct = default)
    {
        var result = await _context.LibraryItems
            .Where(li => li.FileSizeBytes.HasValue)
            .SumAsync(li => (long?)li.FileSizeBytes, ct);

        return result ?? 0L;
    }

    public async Task<long> GetSizeByGradeLevelAsync(
        int gradeLevelId,
        CancellationToken ct = default)
    {
        var result = await _context.LibraryItems
            .Where(li =>
                li.GradeLevelId    == gradeLevelId &&
                li.FileSizeBytes.HasValue)
            .SumAsync(li => (long?)li.FileSizeBytes, ct);

        return result ?? 0L;
    }
}



