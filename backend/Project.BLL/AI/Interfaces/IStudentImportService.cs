using Common.Results;
using Project.BLL.AI.Models;

namespace Project.BLL.AI.Interfaces;

public interface IStudentImportService
{
    Task<OperationResult<ImportResult>> ImportFromExcelAsync(Stream fileStream, int classId, int academicYearId, CancellationToken ct = default);
    Task<OperationResult<ImportResult>> PreviewImportAsync(Stream fileStream, CancellationToken ct = default);
}
