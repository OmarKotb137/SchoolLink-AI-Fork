using Common.Results;
using Project.BLL.DTOs;

namespace Project.BLL.AI.Interfaces;

public interface IBookParserService
{
    Task<OperationResult<List<ParsedUnitDto>>> PreviewBookAsync(Stream pdfStream, string fileName);
    Task<OperationResult<List<UnitDto>>> SaveBookStructureAsync(int subjectId, List<CreateUnitDto> units);
    Task<OperationResult<string>> CleanLessonContentWithAiAsync(string rawContent, string title);
}
