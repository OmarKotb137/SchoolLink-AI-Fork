using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.DAL.Interfaces.Repositories.Timetable;

public interface IRoomRepository : IRepository<Room>
{
    Task<Room?>                    GetByNameAndTypeAsync(string name, string type, CancellationToken ct = default);
    Task<IReadOnlyList<Room>>      GetByTypeAsync(string type, CancellationToken ct = default);
    Task<IReadOnlyList<Room>>      GetAllOrderedAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Room>>      GetAvailableAsync(SchoolDay day, int periodNumber, string? type = null, CancellationToken ct = default);
    Task<bool>                     IsInUseAsync(int roomId, CancellationToken ct = default);
}
