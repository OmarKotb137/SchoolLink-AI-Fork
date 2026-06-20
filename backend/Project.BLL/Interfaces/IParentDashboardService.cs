using Common.Results;
using Project.BLL.DTOs.Dashboard;

namespace Project.BLL.Interfaces;

public interface IParentDashboardService
{
    Task<OperationResult<ParentDashboardDto>> GetParentDashboardAsync(int parentId, int? term = null);
    Task<OperationResult<ParentChildDto>> GetStudentDashboardAsync(int studentId, int? term = null);
}
