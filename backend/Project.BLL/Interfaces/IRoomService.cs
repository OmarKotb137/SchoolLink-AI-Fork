using Common.Results;
using Project.BLL.DTOs;
using Project.Domain.Enums;

namespace Project.BLL.Interfaces;

public interface IRoomService
{
    Task<OperationResult<RoomDto>>              CreateRoomAsync(CreateRoomRequest request);
    Task<OperationResult<RoomDto>>              UpdateRoomAsync(UpdateRoomRequest request);
    Task<OperationResult>                       DeleteRoomAsync(int id);
    Task<OperationResult<RoomDto>>              GetRoomByIdAsync(int id);
    Task<OperationResult<IEnumerable<RoomDto>>> GetAllRoomsAsync();
    Task<OperationResult<IEnumerable<RoomDto>>> GetAvailableRoomsAsync(SchoolDay day, int periodNumber, string? type = null);
    Task<OperationResult<IEnumerable<RoomDto>>> GetRoomsByTypeAsync(string type);
    Task<OperationResult<IEnumerable<TimetableSlotDto>>> GetRoomScheduleAsync(int roomId, SchoolDay day);
}
