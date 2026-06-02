using Common.Results;
using Project.BLL.DTOs.Common;
using Project.BLL.DTOs.Library;

namespace Project.BLL.Interfaces;

public interface ILibraryService
{
    Task<OperationResult<LibraryItemDto>> CreateLibraryItemAsync(CreateLibraryItemRequest request);
    Task<OperationResult<LibraryItemDto>> UpdateLibraryItemAsync(UpdateLibraryItemRequest request);
    Task<OperationResult<PagedResult<LibraryItemDto>>> GetLibraryItemsAsync(GetLibraryFilter filter);
    Task<OperationResult<IEnumerable<LibraryItemDto>>> SearchLibraryAsync(string searchTerm, int gradeLevelId);
    Task<OperationResult> DeleteLibraryItemAsync(int id, int callerUserId);
}
