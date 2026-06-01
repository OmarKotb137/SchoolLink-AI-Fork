using Microsoft.EntityFrameworkCore;
using Project.DAL.Context;
using Project.DAL.Interfaces.Repositories.Settings;
using SchoolLink.Domain.Entities;

namespace Project.DAL.Repositories.Settings;

public class SchoolProfileRepository : Repository<SchoolProfile>, ISchoolProfileRepository
{
    public SchoolProfileRepository(AppDbContext context) : base(context) { }

    public async Task<SchoolProfile?> GetActiveAsync(CancellationToken ct = default)
        => await _context.SchoolProfiles
            .FirstOrDefaultAsync(x => x.IsActive, ct);

    public async Task UpsertActiveAsync(SchoolProfile profile, CancellationToken ct = default)
    {
        var existing = await _context.SchoolProfiles
            .FirstOrDefaultAsync(x => x.IsActive, ct);

        if (existing is null)
        {
            profile.IsActive = true;
            await _context.SchoolProfiles.AddAsync(profile, ct);
            return;
        }

        existing.SchoolName = profile.SchoolName;
        existing.Governorate = profile.Governorate;
        existing.Directorate = profile.Directorate;
        existing.EducationalAdministration = profile.EducationalAdministration;
        existing.Address = profile.Address;
        existing.Phone = profile.Phone;
        existing.Email = profile.Email;
        existing.ManagerName = profile.ManagerName;
        existing.LogoPath = profile.LogoPath;
        existing.UpdatedAt = DateTime.UtcNow;
    }
}
