using Project.Domain.Enums;

namespace Project.Domain.Entities
{
    public class Announcement : BaseEntity
    {
        public int AuthorId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public UserRole? TargetRole { get; set; }
        public int? TargetClassId { get; set; }
        public AnnouncementType? Category { get; set; }
        public int? TargetGradeLevelId { get; set; }
        public bool IsForAllUsers { get; set; }
        public bool IsForAllStudents { get; set; }
        public bool IsForAllParents { get; set; }
        public bool IsForAllTeachers { get; set; }
        public DateTime? ExpiresAt { get; set; }

        // Navigation Properties
        public User Author { get; set; } = null!;
        public SchoolClass? TargetClass { get; set; }
    }
}
