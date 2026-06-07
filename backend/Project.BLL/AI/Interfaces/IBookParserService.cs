using Common.Results;
using Project.BLL.DTOs;

namespace Project.BLL.AI.Interfaces;

public interface IBookParserService
{
    Task<OperationResult<BookPreviewResult>> PreviewBookAsync(Stream pdfStream, string fileName);
    Task<OperationResult<List<UnitDto>>> SaveBookStructureAsync(int subjectId, List<CreateUnitDto> units);
    Task<OperationResult<string>> CleanLessonContentWithAiAsync(string rawContent, string title);
    Task<OperationResult<string>> ReExtractLessonContentAsync(string previewId, string lessonTitle, int pageStart, int? pageEnd);
    Task<OperationResult<string>> ReExtractUnitContentAsync(string previewId, string unitName, int pageStart, int? pageEnd);
}
