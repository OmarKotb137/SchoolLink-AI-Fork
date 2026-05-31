using SchoolLink.Domain.Enums;

namespace SchoolLink.Domain.Entities
{
    public class LibraryItem : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public LibraryItemType ItemType { get; set; }
        public string? FileUrl { get; set; }
        public int? SubjectId { get; set; }
        public int? GradeLevelId { get; set; }
        public int? AcademicYearId { get; set; }
        public int UploadedById { get; set; }
        public bool IsActive { get; set; } = true;
        public long? FileSizeBytes { get; set; }

        // Navigation Properties
        public Subject? Subject { get; set; }
        public GradeLevel? GradeLevel { get; set; }
        public AcademicYear? AcademicYear { get; set; }
        public User UploadedBy { get; set; } = null!;
    }
}