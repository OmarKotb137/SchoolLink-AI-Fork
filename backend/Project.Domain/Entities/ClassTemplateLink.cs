namespace Project.Domain.Entities
{
    public class ClassTemplateLink : BaseEntity
    {
        public int ClassId { get; set; }
        public int TemplateId { get; set; }
        public int AcademicYearId { get; set; }

        public SchoolClass Class { get; set; } = null!;
        public EvaluationTemplate Template { get; set; } = null!;
        public AcademicYear AcademicYear { get; set; } = null!;
    }
}