namespace Project.Domain.Entities;

public class BlockedUser
{
    public int Id { get; set; }
    public int BlockerId { get; set; }
    public int BlockedUserId { get; set; }
    public int ConversationId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User Blocker { get; set; } = null!;
    public User Blocked { get; set; } = null!;
    public Conversation Conversation { get; set; } = null!;
}
