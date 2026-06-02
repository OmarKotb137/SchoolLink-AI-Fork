using Project.Domain.Enums;

namespace Project.Domain.Entities
{
    public class Notification : BaseEntity
    {
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public bool IsRead { get; set; } = false;
        public string? DataJson { get; set; }
        public DateTime? ReadAt { get; set; }

        // Navigation Properties
        public User User { get; set; } = null!;
    }
}
