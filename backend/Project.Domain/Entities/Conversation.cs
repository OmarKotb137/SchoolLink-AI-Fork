using Project.Domain.Enums;

namespace Project.Domain.Entities
{
    public class Conversation : BaseEntity
    {
        public string? Title { get; set; }
        public ConversationType Type { get; set; }
        public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
