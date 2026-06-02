using Project.Domain.Enums;

namespace Project.Domain.Entities
{
    public class ResultVisibilitySetting : BaseEntity
    {
        public int AcademicYearId { get; set; }
        public AcademicTerm Term { get; set; }
        public bool IsVisible { get; set; } = false;
        public DateTime? VisibleFrom { get; set; }
        public DateTime? VisibleUntil { get; set; }
        public int ControlledById { get; set; }

        // Navigation Properties
        public AcademicYear AcademicYear { get; set; } = null!;
        public User ControlledBy { get; set; } = null!;
    }
}
