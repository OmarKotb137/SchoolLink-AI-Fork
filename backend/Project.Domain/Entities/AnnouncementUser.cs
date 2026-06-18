namespace Project.Domain.Entities
{
    public class AnnouncementUser : BaseEntity
    {
        public int AnnouncementId { get; set; }
        public int UserId { get; set; }

        // Navigation Properties
        public Announcement Announcement { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
