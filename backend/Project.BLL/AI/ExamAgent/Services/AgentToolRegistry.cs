using Project.BLL.AI.ExamAgent.Interfaces;

namespace Project.BLL.AI.ExamAgent.Services;

public class AgentToolRegistry
{
    private readonly Dictionary<string, IAgentTool> _tools;

    public AgentToolRegistry(IEnumerable<IAgentTool> tools)
        => _tools = tools.ToDictionary(t => t.Name);

    public IAgentTool? Get(string name)
        => _tools.GetValueOrDefault(name);

    public IEnumerable<IAgentTool> All => _tools.Values;
}
