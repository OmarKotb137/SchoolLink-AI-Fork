using System.Text.Json;

namespace Project.BLL.AI.Models;

public class AiTool
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public JsonElement? Parameters { get; set; }
    public Func<string, Task<string>>? ExecuteAsync { get; set; }
}
