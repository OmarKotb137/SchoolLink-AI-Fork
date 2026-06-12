namespace Project.BLL.DTOs.Conversations;

public class MessageDto
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public string? AttachmentType { get; set; }
    public string? VoiceText { get; set; }
    public DateTime SentAt { get; set; }
    public bool IsEdited { get; set; }
}

