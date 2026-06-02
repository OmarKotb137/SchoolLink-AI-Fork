using Microsoft.EntityFrameworkCore;
using Project.DAL.Context;
using Project.DAL.Interfaces.Repositories.Timetable;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.DAL.Repositories.Timetable;

public class RoomRepository : Repository<Room>, IRoomRepository
{
    public RoomRepository(AppDbContext context) : base(context) { }

    public async Task<Room?> GetByNameAndTypeAsync(
        string name,
        RoomType type,
        CancellationToken ct = default)
        => await _context.Rooms
            .FirstOrDefaultAsync(r => r.Name == name && r.Type == type, ct);

    public async Task<IReadOnlyList<Room>> GetByTypeAsync(
        RoomType type,
        CancellationToken ct = default)
        => await _context.Rooms
            .Where(r => r.Type == type)
            .OrderBy(r => r.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Room>> GetAllOrderedAsync(
        CancellationToken ct = default)
        => await _context.Rooms
            .OrderBy(r => r.Type)
            .ThenBy(r => r.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Room>> GetAvailableAsync(
        SchoolDay day,
        int periodNumber,
        RoomType? type = null,
        CancellationToken ct = default)
        => await _context.Rooms
            .Where(r => !type.HasValue || r.Type == type.Value)
            .Where(r => !_context.TimetableSlots.Any(s =>
                s.RoomId == r.Id &&
                s.DayOfWeek == day &&
                s.PeriodNumber == periodNumber &&
                s.Timetable.IsActive))
            .OrderBy(r => r.Type)
            .ThenBy(r => r.Name)
            .ToListAsync(ct);

    public async Task<bool> IsInUseAsync(
        int roomId,
        CancellationToken ct = default)
        => await _context.TimetableSlots
            .AnyAsync(s => s.RoomId == roomId, ct);
}
