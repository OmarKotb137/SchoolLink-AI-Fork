using Project.Domain.Entities;

namespace Project.DAL.Interfaces.Repositories.Settings;

public interface ISchoolProfileRepository : IRepository<SchoolProfile>
{
    Task<SchoolProfile?> GetActiveAsync(CancellationToken ct = default);
    Task UpsertActiveAsync(SchoolProfile profile, CancellationToken ct = default);
}
