namespace Project.Domain.Entities
{
    public class Message : BaseEntity
    {
        public int ConversationId { get; set; }
        public int SenderId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? AttachmentUrl { get; set; }
        public string? AttachmentType { get; set; }
        public string? VoiceText { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public bool IsEdited { get; set; }
        public DateTime? EditedAt { get; set; }

        // Navigation Properties
        public Conversation Conversation { get; set; } = null!;
        public User Sender { get; set; } = null!;
    }
}
