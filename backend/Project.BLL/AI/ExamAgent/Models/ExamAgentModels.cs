namespace Project.BLL.AI.ExamAgent.Models;

public class Lesson
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Content { get; set; } = "";
}

public class ExamRequest
{
    public string LessonContent { get; set; } = "";
    public int QuestionCount { get; set; } = 5;
    public string Difficulty { get; set; } = "medium";
    public string Style { get; set; } = "multiple_choice";
}

public class ExamResponse
{
    public string Content { get; set; } = "";
}

public class AgentRequest
{
    public string UserMessage { get; set; } = "";
}

public class AgentChatResponse
{
    public string Answer { get; set; } = "";
}

public class ChatRequest
{
    public string Message { get; set; } = "";
    public string? ConversationId { get; set; }
}

public class SaveMessageRequest
{
    public string ConversationId { get; set; } = "";
    public string Content { get; set; } = "";
    public string Sender { get; set; } = "";
    public DateTime Timestamp { get; set; }
}

public class ConversationMessage
{
    public int Id { get; set; }
    public string ConversationId { get; set; } = "";
    public string Content { get; set; } = "";
    public string Sender { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class Conversation
{
    public int Id { get; set; }
    public string ConversationId { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
    public List<ConversationMessage> Messages { get; set; } = [];
    public string Title { get; set; } = "";
}

public enum LlmProvider { OpenRouter, HuggingFace, CloudflareAI, OpenCodeAI }
