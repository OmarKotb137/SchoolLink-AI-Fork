namespace Project.BLL.AI.Models;

public class AgentResponse
{
    public string Text { get; set; } = string.Empty;
    public List<string> SuggestedActions { get; set; } = new();
    public Dictionary<string, object>? AdditionalData { get; set; }
}
