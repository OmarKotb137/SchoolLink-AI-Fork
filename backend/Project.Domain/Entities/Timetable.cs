namespace Project.Domain.Entities
{
    public class Timetable : BaseEntity
    {
        public int ClassId { get; set; }
        public int AcademicYearId { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public SchoolClass Class { get; set; } = null!;
        public AcademicYear AcademicYear { get; set; } = null!;
        public ICollection<TimetableSlot> Slots { get; set; } = new List<TimetableSlot>();
    }
}
