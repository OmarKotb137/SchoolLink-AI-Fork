namespace Project.Domain.Entities
{
    public class SchoolClass : BaseEntity
    {
        public int GradeLevelId { get; set; }
        public int AcademicYearId { get; set; }
        public string Name { get; set; } = string.Empty;

        // Navigation Properties
        public GradeLevel GradeLevel { get; set; } = null!;
        public AcademicYear AcademicYear { get; set; } = null!;
        public ICollection<StudentEnrollment> Enrollments { get; set; } = new List<StudentEnrollment>();
    }
}
