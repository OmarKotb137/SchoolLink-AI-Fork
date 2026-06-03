using AutoMapper;
using Common.Results;
using Project.BLL.DTOs.Common;
using Project.BLL.DTOs.Library;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.Services;

public class LibraryService : ILibraryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public LibraryService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<OperationResult<LibraryItemDto>> CreateLibraryItemAsync(CreateLibraryItemRequest request)
    {
        var uploader = await _unitOfWork.Users.GetByIdAsync(request.UploadedById);
        if (uploader == null || uploader.IsDeleted)
            return OperationResult<LibraryItemDto>.Failure("لم يتم العثور على الرافع");

        if (uploader.Role != UserRole.Admin && uploader.Role != UserRole.Teacher)
            return OperationResult<LibraryItemDto>.Failure("فقط المدراء والمدرسون يمكنهم رفع عناصر المكتبة");

        if (string.IsNullOrWhiteSpace(request.Title))
            return OperationResult<LibraryItemDto>.Failure("العنوان مطلوب");

        if (request.ItemType is LibraryItemType.Book or LibraryItemType.File or LibraryItemType.Video
            && string.IsNullOrWhiteSpace(request.FileUrl))
            return OperationResult<LibraryItemDto>.Failure("رابط الملف مطلوب لهذا النوع من العناصر");

        if (request.SubjectId.HasValue)
        {
            var subject = await _unitOfWork.Subjects.GetByIdAsync(request.SubjectId.Value);
            if (subject == null || subject.IsDeleted)
                return OperationResult<LibraryItemDto>.Failure("لم يتم العثور على المادة");
        }

        if (request.GradeLevelId.HasValue)
        {
            var gradeLevel = await _unitOfWork.GradeLevels.GetByIdAsync(request.GradeLevelId.Value);
            if (gradeLevel == null || gradeLevel.IsDeleted)
                return OperationResult<LibraryItemDto>.Failure("لم يتم العثور على المستوى الدراسي");
        }

        if (request.AcademicYearId.HasValue)
        {
            var year = await _unitOfWork.AcademicYears.GetByIdAsync(request.AcademicYearId.Value);
            if (year == null || year.IsDeleted)
                return OperationResult<LibraryItemDto>.Failure("لم يتم العثور على السنة الدراسية");
        }

        var item = _mapper.Map<LibraryItem>(request);
        await _unitOfWork.LibraryItems.AddAsync(item);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<LibraryItemDto>(item);
        dto.UploadedByName = uploader.FullName;

        return OperationResult<LibraryItemDto>.Success(dto, "تم رفع عنصر المكتبة بنجاح");
    }

    public async Task<OperationResult<LibraryItemDto>> UpdateLibraryItemAsync(UpdateLibraryItemRequest request)
    {
        var item = await _unitOfWork.LibraryItems.GetByIdAsync(request.Id);
        if (item == null || item.IsDeleted)
            return OperationResult<LibraryItemDto>.Failure($"عنصر المكتبة ذو المعرف {request.Id} غير موجود");

        var caller = await _unitOfWork.Users.GetByIdAsync(request.CallerUserId);
        if (caller == null || caller.IsDeleted)
            return OperationResult<LibraryItemDto>.Failure("لم يتم العثور على المستدعي");

        if (caller.Role != UserRole.Admin && item.UploadedById != request.CallerUserId)
            return OperationResult<LibraryItemDto>.Failure("فقط الرافع أو المدراء يمكنهم تحديث هذا العنصر");

        if (string.IsNullOrWhiteSpace(request.Title))
            return OperationResult<LibraryItemDto>.Failure("العنوان مطلوب");

        item.Title = request.Title;
        item.Description = request.Description;
        item.IsActive = request.IsActive;
        item.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.LibraryItems.Update(item);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<LibraryItemDto>(item);
        return OperationResult<LibraryItemDto>.Success(dto, "تم تحديث عنصر المكتبة بنجاح");
    }

    public async Task<OperationResult<LibraryItemDto>> GetLibraryItemByIdAsync(int id)
    {
        var item = await _unitOfWork.LibraryItems.GetByIdAsync(id);
        if (item == null || item.IsDeleted)
            return OperationResult<LibraryItemDto>.Failure("عنصر المكتبة غير موجود");

        var dto = _mapper.Map<LibraryItemDto>(item);
        return OperationResult<LibraryItemDto>.Success(dto);
    }

    public async Task<OperationResult<LibraryStatsDto>> GetLibraryStatsAsync()
    {
        var totalItems = await _unitOfWork.LibraryItems.CountAsync(i => !i.IsDeleted);
        var totalSize = await _unitOfWork.LibraryItems.GetTotalSizeBytesAsync();

        var items = await _unitOfWork.LibraryItems.GetActiveAsync();
        var grouped = items.GroupBy(i => i.ItemType).ToDictionary(g => g.Key, g => g.Count());

        var stats = new LibraryStatsDto
        {
            TotalItems = totalItems,
            TotalSizeBytes = totalSize,
            BooksCount = grouped.GetValueOrDefault(LibraryItemType.Book, 0),
            FilesCount = grouped.GetValueOrDefault(LibraryItemType.File, 0),
            VideosCount = grouped.GetValueOrDefault(LibraryItemType.Video, 0),
            LinksCount = grouped.GetValueOrDefault(LibraryItemType.Link, 0),
            NotesCount = grouped.GetValueOrDefault(LibraryItemType.Note, 0)
        };

        return OperationResult<LibraryStatsDto>.Success(stats);
    }

    public async Task<OperationResult<IEnumerable<LibraryItemDto>>> GetLatestLibraryItemsAsync(int count)
    {
        var items = await _unitOfWork.LibraryItems.GetActiveAsync();
        var latest = items
            .OrderByDescending(i => i.CreatedAt)
            .Take(count)
            .ToList();

        var dtos = _mapper.Map<IEnumerable<LibraryItemDto>>(latest);
        return OperationResult<IEnumerable<LibraryItemDto>>.Success(dtos);
    }

    public async Task<OperationResult<PagedResult<LibraryItemDto>>> GetLibraryItemsAsync(GetLibraryFilter filter)
    {
        var query = (await _unitOfWork.LibraryItems.GetActiveAsync()).AsQueryable();

        if (filter.SubjectId.HasValue)
            query = query.Where(i => i.SubjectId == filter.SubjectId.Value);

        if (filter.GradeLevelId.HasValue)
            query = query.Where(i => i.GradeLevelId == filter.GradeLevelId.Value);

        if (filter.AcademicYearId.HasValue)
            query = query.Where(i => i.AcademicYearId == filter.AcademicYearId.Value);

        if (filter.ItemType.HasValue)
            query = query.Where(i => i.ItemType == filter.ItemType.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(i => i.Title.ToLower().Contains(term)
                || (i.Description != null && i.Description.ToLower().Contains(term)));
        }

        var totalCount = query.Count();

        var items = query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();

        var dtos = _mapper.Map<IEnumerable<LibraryItemDto>>(items);

        return OperationResult<PagedResult<LibraryItemDto>>.Success(new PagedResult<LibraryItemDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        });
    }

    public async Task<OperationResult<IEnumerable<LibraryItemDto>>> SearchLibraryAsync(string searchTerm, int gradeLevelId)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
            return OperationResult<IEnumerable<LibraryItemDto>>.Failure("يجب أن يتكون مصطلح البحث من حرفين على الأقل");

        var results = await _unitOfWork.LibraryItems.SearchByTitleAsync(searchTerm);

        var filtered = results
            .Where(i => !i.IsDeleted && i.IsActive)
            .Where(i => i.GradeLevelId == gradeLevelId || i.GradeLevelId == null)
            .OrderByDescending(i => i.CreatedAt);

        var dtos = _mapper.Map<IEnumerable<LibraryItemDto>>(filtered);
        return OperationResult<IEnumerable<LibraryItemDto>>.Success(dtos);
    }

    public async Task<OperationResult<PagedResult<LibraryItemDto>>> GetLibraryItemsBySubjectAsync(int subjectId, PaginationFilter filter)
    {
        var items = await _unitOfWork.LibraryItems.GetBySubjectIdAsync(subjectId);
        var filtered = items.Where(i => !i.IsDeleted && i.IsActive)
            .OrderByDescending(i => i.CreatedAt).ToList();

        var totalCount = filtered.Count;
        var paged = filtered.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToList();
        var dtos = _mapper.Map<IEnumerable<LibraryItemDto>>(paged);

        return OperationResult<PagedResult<LibraryItemDto>>.Success(new PagedResult<LibraryItemDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        });
    }

    public async Task<OperationResult<PagedResult<LibraryItemDto>>> GetLibraryItemsByUploaderAsync(int uploaderId, PaginationFilter filter)
    {
        var items = await _unitOfWork.LibraryItems.GetByUploaderIdAsync(uploaderId);
        var filtered = items.Where(i => !i.IsDeleted)
            .OrderByDescending(i => i.CreatedAt).ToList();

        var totalCount = filtered.Count;
        var paged = filtered.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToList();
        var dtos = _mapper.Map<IEnumerable<LibraryItemDto>>(paged);

        return OperationResult<PagedResult<LibraryItemDto>>.Success(new PagedResult<LibraryItemDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        });
    }

    public async Task<OperationResult> DeleteLibraryItemAsync(int id, int callerUserId)
    {
        var item = await _unitOfWork.LibraryItems.GetByIdAsync(id);
        if (item == null || item.IsDeleted)
            return OperationResult.Failure($"عنصر المكتبة ذو المعرف {id} غير موجود");

        var caller = await _unitOfWork.Users.GetByIdAsync(callerUserId);
        if (caller == null || caller.IsDeleted)
            return OperationResult.Failure("لم يتم العثور على المستدعي");

        if (caller.Role != UserRole.Admin && item.UploadedById != callerUserId)
            return OperationResult.Failure("فقط الرافع أو المدراء يمكنهم حذف هذا العنصر");

        _unitOfWork.LibraryItems.SoftDelete(item);
        await _unitOfWork.SaveChangesAsync();
        return OperationResult.Success("تم حذف عنصر المكتبة بنجاح");
    }
}
