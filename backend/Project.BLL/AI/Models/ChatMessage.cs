namespace Project.BLL.AI.Models;

public class ChatMessage
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;

    public MessageRole ToMessageRole() => Role switch
    {
        "user" => MessageRole.User,
        "assistant" => MessageRole.Assistant,
        "system" => MessageRole.System,
        _ => MessageRole.User
    };
}
