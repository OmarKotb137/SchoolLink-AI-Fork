namespace Project.Domain.Entities
{
    public class ConversationParticipant : BaseEntity
    {
        public int ConversationId { get; set; }
        public int UserId { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastReadAt { get; set; }

        // Navigation Properties
        public Conversation Conversation { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
