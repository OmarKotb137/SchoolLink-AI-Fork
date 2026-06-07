using Common.Results;
using Project.BLL.DTOs.Dashboard;

namespace Project.BLL.Interfaces;

public interface IDashboardService
{
    Task<OperationResult<AdminDashboardDto>> GetAdminDashboardAsync(int? academicYearId = null);
}
