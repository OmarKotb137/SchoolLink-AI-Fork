using Project.Domain.Entities;

namespace Project.BLL.Interfaces
{
    public interface IExamMediaService
    {
        Task<string> GenerateImageAsync(string imagePrompt, int groupId, CancellationToken ct = default);
        string SanitizeSvg(string rawSvg);
    }
}
