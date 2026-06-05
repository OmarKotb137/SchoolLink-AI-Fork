using Project.Domain.Entities;

namespace Project.BLL.Interfaces
{
    public interface IExamHtmlRenderer
    {
        Task<string> RenderExamAsync(int examId, CancellationToken ct = default);
    }
}
