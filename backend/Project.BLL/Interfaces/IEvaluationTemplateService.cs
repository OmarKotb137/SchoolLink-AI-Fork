using Common.Results;
using Project.BLL.DTOs.EvaluationTemplates;

namespace Project.BLL.Interfaces;

public interface IEvaluationTemplateService
{
    Task<OperationResult<EvaluationTemplateDto>> CreateEvaluationTemplateAsync(CreateEvaluationTemplateRequest request);
    Task<OperationResult<EvaluationTemplateDto>> UpdateEvaluationTemplateAsync(UpdateEvaluationTemplateRequest request);
    Task<OperationResult<EvaluationTemplateDto>> GetTemplateByIdAsync(int id);
    Task<OperationResult<IEnumerable<EvaluationTemplateDto>>> GetTemplateByGradeLevelAsync(int gradeLevelId, int academicYearId);
    Task<OperationResult> DeleteEvaluationTemplateAsync(int id);
}
