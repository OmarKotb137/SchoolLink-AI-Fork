using Project.Domain.Enums;

namespace Project.Domain.Entities
{
    public class Unit : BaseEntity
    {
        public int SubjectId { get; set; }
        public int GradeLevelId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Content { get; set; }
        public int DisplayOrder { get; set; }
        public int? PageStart { get; set; }
        public int? PageEnd { get; set; }
        public AcademicTerm? Term { get; set; } // null = both semesters, 1 = FirstSemester, 2 = SecondSemester

        public Subject Subject { get; set; } = null!;
        public GradeLevel GradeLevel { get; set; } = null!;
        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    }
}
