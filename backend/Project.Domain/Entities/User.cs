using Project.Domain.Enums;

namespace Project.Domain.Entities
{
    public class User : BaseEntity
    {
        public string FullName { get; set; } = string.Empty;

        // ✅ كان Email — أصبح Username للـ login
        public string Username { get; set; } = string.Empty;

        // ✅ جديد — Email حقيقي اختياري للإشعارات
        public string? ContactEmail { get; set; }
        public bool IsContactEmailVerified { get; set; }
        public DateTime? ContactEmailVerifiedAt { get; set; }

        public string? Phone { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool IsActive { get; set; } = true;
        public string? ProfilePictureUrl { get; set; }
    }
}
