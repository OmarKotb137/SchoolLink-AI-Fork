using AutoMapper;
using Common.Results;
using Project.BLL.DTOs;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Enums;
using SchoolLink.Domain.Entities;
using SchoolLink.Domain.Enums;

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
        // 1. Name + Type uniqueness
        var existing = await _unitOfWork.Rooms.GetByNameAndTypeAsync(request.Name, request.Type);
        if (existing is not null && !existing.IsDeleted)
            return OperationResult<RoomDto>.Failure("غرفة بنفس الاسم والنوع موجودة بالفعل");

        // 2. Create
        var entity = _mapper.Map<Room>(request);
        await _unitOfWork.Rooms.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult<RoomDto>.Success(
            _mapper.Map<RoomDto>(entity),
            "تم إنشاء الغرفة بنجاح");
    }

    public async Task<OperationResult<RoomDto>> UpdateRoomAsync(
        UpdateRoomRequest request)
    {
        // 1. Find entity
        var entity = await _unitOfWork.Rooms.GetByIdAsync(request.Id);
        if (entity is null || entity.IsDeleted)
            return OperationResult<RoomDto>.Failure("الغرفة غير موجودة");

        // 2. Name + Type uniqueness (excluding self)
        var duplicate = await _unitOfWork.Rooms.GetByNameAndTypeAsync(request.Name, request.Type);
        if (duplicate is not null && !duplicate.IsDeleted && duplicate.Id != request.Id)
            return OperationResult<RoomDto>.Failure("غرفة بنفس الاسم والنوع موجودة بالفعل");

        // 3. Apply updates
        entity.Name      = request.Name;
        entity.Type      = request.Type;
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

    public async Task<OperationResult<IEnumerable<RoomDto>>> GetAvailableRoomsAsync(
        SchoolDay day,
        int periodNumber,
        RoomType? type = null)
    {
        if (periodNumber < 1)
            return OperationResult<IEnumerable<RoomDto>>.Failure("رقم الحصة غير صالح");

        var rooms = await _unitOfWork.Rooms.GetAvailableAsync(day, periodNumber, type);

        return OperationResult<IEnumerable<RoomDto>>.Success(
            _mapper.Map<IEnumerable<RoomDto>>(rooms),
            "تم جلب الغرف المتاحة بنجاح");
    }
}
