namespace SchoolLink.Domain.Entities
{
    public class ClassSubjectTeacher : BaseEntity
    {
        public int ClassId { get; set; }
        public int SubjectId { get; set; }
        public int TeacherId { get; set; }
        public int AcademicYearId { get; set; }

        // Navigation Properties
        public SchoolClass Class { get; set; } = null!;
        public Subject Subject { get; set; } = null!;
        public User Teacher { get; set; } = null!;
        public AcademicYear AcademicYear { get; set; } = null!;
    }
}
