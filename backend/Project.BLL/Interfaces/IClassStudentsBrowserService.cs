using Common.Results;
using Project.BLL.DTOs.ClassStudentsBrowser;

namespace Project.BLL.Interfaces;

public interface IClassStudentsBrowserService
{
    Task<OperationResult<ClassStudentsBrowserResultDto>> GetClassStudentsAsync(
        int classId,
        GetClassStudentsBrowserFilter filter);
}
