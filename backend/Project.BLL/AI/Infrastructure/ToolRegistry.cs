using System.Collections.Concurrent;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;

namespace Project.BLL.AI.Infrastructure;

public class ToolRegistry : IToolRegistry
{
    private readonly ConcurrentDictionary<string, AiTool> _tools = new();

    public void RegisterTool(AiTool tool)
    {
        _tools[tool.Name] = tool;
    }

    public void RegisterTools(IEnumerable<AiTool> tools)
    {
        foreach (var tool in tools)
            _tools[tool.Name] = tool;
    }

    public List<AiTool> GetToolsForAgent(string agentType)
    {
        return _tools.Values
            .Where(t => t.Name.StartsWith(agentType, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public AiTool? FindTool(string name)
    {
        _tools.TryGetValue(name, out var tool);
        return tool;
    }
}
