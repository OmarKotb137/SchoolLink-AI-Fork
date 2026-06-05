using Project.BLL.AI.Models;

namespace Project.BLL.AI.Interfaces;

public interface IToolRegistry
{
    void RegisterTool(AiTool tool);
    void RegisterTools(IEnumerable<AiTool> tools);
    List<AiTool> GetToolsForAgent(string agentType);
    AiTool? FindTool(string name);
}
