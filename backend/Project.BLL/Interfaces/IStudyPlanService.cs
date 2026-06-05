using Common.Results;
using Project.BLL.DTOs.StudyPlans;

namespace Project.BLL.Interfaces;

public interface IStudyPlanService
{
    Task<OperationResult<StudyPlanDto>> GenerateStudyPlanWithAIAsync(GenerateStudyPlanRequest request);
    Task<OperationResult<StudyPlanDto>> CreateManualStudyPlanAsync(CreateStudyPlanRequest request);
    Task<OperationResult<StudyPlanDto>> GetActiveStudyPlanAsync(int enrollmentId);
    Task<OperationResult> MarkSessionCompleteAsync(int studyPlanItemId, int enrollmentId);
    Task<OperationResult> MarkSessionIncompleteAsync(int studyPlanItemId, int enrollmentId);
    Task<OperationResult<IEnumerable<StudyPlanSummaryDto>>> GetAllStudyPlansAsync(int enrollmentId);
    Task<OperationResult> DeactivateStudyPlanAsync(int studyPlanId);
    Task<OperationResult<StudyPlanItemDto>> UpdateStudyPlanItemAsync(UpdateStudyPlanItemRequest request);
}
