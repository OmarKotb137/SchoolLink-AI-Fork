using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Settings;
using Project.Domain.Entities;
using Project.Domain.Enums;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Settings;

public class ResultVisibilitySettingRepository
    : Repository<ResultVisibilitySetting>, IResultVisibilitySettingRepository
{
    public ResultVisibilitySettingRepository(AppDbContext context) : base(context) { }


    public async Task<ResultVisibilitySetting?> GetByAcademicYearAndTermAsync(
        int academicYearId,
        AcademicTerm term,
        CancellationToken ct = default)
        => await _context.ResultVisibilitySettings
            .Include(s => s.ControlledBy)
            .FirstOrDefaultAsync(s =>
                s.AcademicYearId == academicYearId &&
                s.Term           == term, ct);


    public async Task<bool> IsVisibleAsync(
        int academicYearId,
        AcademicTerm term,
        CancellationToken ct = default)
    {
        var setting = await _context.ResultVisibilitySettings
            .FirstOrDefaultAsync(s =>
                s.AcademicYearId == academicYearId &&
                s.Term           == term, ct);

        if (setting is null)     return false;
        if (!setting.IsVisible)  return false;

        var now = DateTime.UtcNow;

        if (setting.VisibleFrom.HasValue   && now < setting.VisibleFrom.Value)  return false;
        if (setting.VisibleUntil.HasValue  && now > setting.VisibleUntil.Value) return false;

        return true;
    }

    public async Task<bool> ExistsByYearAndTermAsync(
        int academicYearId,
        AcademicTerm term,
        CancellationToken ct = default)
        => await _context.ResultVisibilitySettings
            .AnyAsync(s =>
                s.AcademicYearId == academicYearId &&
                s.Term           == term, ct);


    public async Task<IReadOnlyList<ResultVisibilitySetting>> GetByAcademicYearIdAsync(
        int academicYearId,
        CancellationToken ct = default)
        => await _context.ResultVisibilitySettings
            .Where(s => s.AcademicYearId == academicYearId)
            .Include(s => s.ControlledBy)
            .OrderBy(s => s.Term)
            .ToListAsync(ct);


    public async Task UpsertAsync(
        ResultVisibilitySetting setting,
        CancellationToken ct = default)
    {
        var existing = await _context.ResultVisibilitySettings
            .FirstOrDefaultAsync(s =>
                s.AcademicYearId == setting.AcademicYearId &&
                s.Term           == setting.Term, ct);

        if (existing is null)
            await _context.ResultVisibilitySettings.AddAsync(setting, ct);
        else
        {
            existing.IsVisible      = setting.IsVisible;
            existing.VisibleFrom    = setting.VisibleFrom;
            existing.VisibleUntil   = setting.VisibleUntil;
            existing.ControlledById = setting.ControlledById;
            existing.UpdatedAt      = DateTime.UtcNow;
        }
    }
}



