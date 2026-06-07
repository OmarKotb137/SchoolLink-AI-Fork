using Common.Results;
using Project.BLL.AI.Models;

namespace Project.BLL.AI.Interfaces;

public interface IStudentImportService
{
    Task<OperationResult<ImportPreviewResult>> PreviewImportAsync(List<FileData> files, CancellationToken ct = default);
    Task<OperationResult<ImportResult>> ImportWithAiAsync(List<ImportedStudentDto> students, int classId, int? academicYearId, CancellationToken ct = default);
}
