using Common.Results;
using Project.BLL.DTOs.EvaluationItems;

namespace Project.BLL.Interfaces;

public interface IEvaluationItemService
{
    Task<OperationResult<EvaluationItemDto>> CreateEvaluationItemAsync(CreateEvaluationItemRequest request);
    Task<OperationResult<EvaluationItemDto>> UpdateEvaluationItemAsync(UpdateEvaluationItemRequest request);
    Task<OperationResult> ToggleItemVisibilityAsync(int id);
    Task<OperationResult> DeleteEvaluationItemAsync(int id);
    Task<OperationResult<IEnumerable<EvaluationItemDto>>> GetItemsByTemplateAsync(int templateId);
    Task<OperationResult<EvaluationItemDto>> GetItemByIdAsync(int id);
    Task<OperationResult> ReorderItemsAsync(int templateId, List<int> orderedIds);
}
