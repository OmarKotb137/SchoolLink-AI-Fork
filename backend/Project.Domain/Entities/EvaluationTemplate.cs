using Project.Domain.Enums;

namespace Project.Domain.Entities
{
    public class EvaluationTemplate : BaseEntity
    {
        public int GradeLevelId { get; set; }
        public int SubjectId { get; set; }
        public int AcademicYearId { get; set; }
        public string Name { get; set; } = string.Empty;
        public EvaluationCalculationType CalculationType { get; set; }
        public bool IsActive { get; set; } = true;
        public int Weeks { get; set; } = 12;
        public AcademicTerm? Term { get; set; }  // null = whole-year, 1 = FirstSemester, 2 = SecondSemester

        // Navigation Properties
        public GradeLevel GradeLevel { get; set; } = null!;
        public Subject Subject { get; set; } = null!;
        public AcademicYear AcademicYear { get; set; } = null!;
        public ICollection<EvaluationItem> Items { get; set; } = new List<EvaluationItem>();
    }
}
