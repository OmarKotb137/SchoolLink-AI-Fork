using Common.Results;
using Project.BLL.DTOs.Dashboard;

namespace Project.BLL.Interfaces;

public interface ITeacherDashboardService
{
    Task<OperationResult<TeacherDashboardDto>> GetTeacherDashboardAsync(int teacherId);
}
