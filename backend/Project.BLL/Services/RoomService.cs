using AutoMapper;
using Common.Results;
using Project.BLL.DTOs;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.Services;

public class RoomService : IRoomService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper     _mapper;

    public RoomService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper     = mapper;
    }

    public async Task<OperationResult<RoomDto>> CreateRoomAsync(
        CreateRoomRequest request)
    {
        var name = request.Name.Trim();
        var type = request.Type.Trim();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(type))
            return OperationResult<RoomDto>.Failure("اسم الغرفة ونوعها مطلوبان");

        // 1. Name + Type uniqueness
        var existing = await _unitOfWork.Rooms.GetByNameAndTypeAsync(name, type);
        if (existing is not null && !existing.IsDeleted)
            return OperationResult<RoomDto>.Failure("غرفة بنفس الاسم والنوع موجودة بالفعل");

        // 2. Create
        var entity = _mapper.Map<Room>(request);
        entity.Name = name;
        entity.Type = type;
        await _unitOfWork.Rooms.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult<RoomDto>.Success(
            _mapper.Map<RoomDto>(entity),
            "تم إنشاء الغرفة بنجاح");
    }

    public async Task<OperationResult<RoomDto>> UpdateRoomAsync(
        UpdateRoomRequest request)
    {
        var name = request.Name.Trim();
        var type = request.Type.Trim();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(type))
            return OperationResult<RoomDto>.Failure("اسم الغرفة ونوعها مطلوبان");

        // 1. Find entity
        var entity = await _unitOfWork.Rooms.GetByIdAsync(request.Id);
        if (entity is null || entity.IsDeleted)
            return OperationResult<RoomDto>.Failure("الغرفة غير موجودة");

        // 2. Name + Type uniqueness (excluding self)
        var duplicate = await _unitOfWork.Rooms.GetByNameAndTypeAsync(name, type);
        if (duplicate is not null && !duplicate.IsDeleted && duplicate.Id != request.Id)
            return OperationResult<RoomDto>.Failure("غرفة بنفس الاسم والنوع موجودة بالفعل");

        // 3. Apply updates
        entity.Name      = name;
        entity.Type      = type;
        entity.Capacity  = request.Capacity;
        entity.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Rooms.Update(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult<RoomDto>.Success(
            _mapper.Map<RoomDto>(entity),
            "تم تحديث الغرفة بنجاح");
    }

    public async Task<OperationResult> DeleteRoomAsync(int id)
    {
        var entity = await _unitOfWork.Rooms.GetByIdAsync(id);
        if (entity is null || entity.IsDeleted)
            return OperationResult.Failure("الغرفة غير موجودة");

        // Protect if in use by any timetable slot
        if (await _unitOfWork.Rooms.IsInUseAsync(id))
            return OperationResult.Failure("لا يمكن حذف غرفة مستخدمة في جدول دراسي");

        _unitOfWork.Rooms.SoftDelete(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("تم حذف الغرفة بنجاح");
    }

    public async Task<OperationResult<RoomDto>> GetRoomByIdAsync(int id)
    {
        var entity = await _unitOfWork.Rooms.GetByIdAsync(id);
        if (entity is null || entity.IsDeleted)
            return OperationResult<RoomDto>.Failure("الغرفة غير موجودة");

        return OperationResult<RoomDto>.Success(
            _mapper.Map<RoomDto>(entity),
            "تم جلب الغرفة بنجاح");
    }

    public async Task<OperationResult<IEnumerable<RoomDto>>> GetAllRoomsAsync()
    {
        var all  = await _unitOfWork.Rooms.GetAllOrderedAsync();
        var list = all.Where(r => !r.IsDeleted);

        return OperationResult<IEnumerable<RoomDto>>.Success(
            _mapper.Map<IEnumerable<RoomDto>>(list),
            "تم جلب الغرف بنجاح");
    }

    public async Task<OperationResult<IEnumerable<RoomDto>>> GetRoomsByTypeAsync(string type)
    {
        var rooms = await _unitOfWork.Rooms.GetByTypeAsync(type);
        var list = rooms.Where(r => !r.IsDeleted);

        return OperationResult<IEnumerable<RoomDto>>.Success(
            _mapper.Map<IEnumerable<RoomDto>>(list),
            "تم جلب الغرف حسب النوع بنجاح");
    }

    public async Task<OperationResult<IEnumerable<TimetableSlotDto>>> GetRoomScheduleAsync(int roomId, SchoolDay day)
    {
        var room = await _unitOfWork.Rooms.GetByIdAsync(roomId);
        if (room is null || room.IsDeleted)
            return OperationResult<IEnumerable<TimetableSlotDto>>.Failure("الغرفة غير موجودة");

        var allTimetables = await _unitOfWork.Timetables.FindAsync(t => !t.IsDeleted);
        var schedule = new List<TimetableSlot>();

        foreach (var tt in allTimetables)
        {
            var slots = await _unitOfWork.TimetableSlots.GetByDayWithDetailsAsync(tt.Id, day);
            schedule.AddRange(slots.Where(s => !s.IsDeleted && s.RoomId == roomId));
        }

        var dtos = _mapper.Map<IEnumerable<TimetableSlotDto>>(schedule.OrderBy(s => s.PeriodNumber));
        return OperationResult<IEnumerable<TimetableSlotDto>>.Success(dtos, "تم جلب جدول الغرفة بنجاح");
    }

    public async Task<OperationResult<IEnumerable<RoomDto>>> GetAvailableRoomsAsync(
        SchoolDay day,
        int periodNumber,
        string? type = null)
    {
        if (periodNumber < 1)
            return OperationResult<IEnumerable<RoomDto>>.Failure("رقم الحصة غير صالح");

        var rooms = await _unitOfWork.Rooms.GetAvailableAsync(day, periodNumber, string.IsNullOrWhiteSpace(type) ? null : type.Trim());

        return OperationResult<IEnumerable<RoomDto>>.Success(
            _mapper.Map<IEnumerable<RoomDto>>(rooms),
            "تم جلب الغرف المتاحة بنجاح");
    }
}
