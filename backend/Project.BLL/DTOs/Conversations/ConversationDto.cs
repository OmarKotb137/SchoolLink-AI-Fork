using Project.Domain.Enums;

namespace Project.BLL.DTOs.Conversations;

public class ConversationDto
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public ConversationType Type { get; set; }
    public DateTime LastMessageAt { get; set; }
    public List<ConversationParticipantDto> Participants { get; set; } = new();
    public MessageDto? LastMessage { get; set; }
}
