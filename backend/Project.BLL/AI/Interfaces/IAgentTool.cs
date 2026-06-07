using System.Text.Json;
using Project.BLL.AI.Models;

namespace Project.BLL.AI.Interfaces;

public interface IAgentTool
{
    string Name { get; }
    string Description { get; }
    FunctionDefinition ToFunctionDefinition();
    Task<ToolResult> ExecuteAsync(JsonElement args);
}
