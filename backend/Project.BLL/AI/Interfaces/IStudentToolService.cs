using Project.BLL.AI.Models;

namespace Project.BLL.AI.Interfaces;

public interface IStudentToolService
{
    Dictionary<string, AiTool> CreateTools(UserContext context, CancellationToken ct = default);
}
