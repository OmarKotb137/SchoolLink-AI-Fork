namespace Project.Domain.Entities
{
    public class Subject : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }

        public ICollection<Unit> Units { get; set; } = new List<Unit>();
        public ICollection<Exam> Exams { get; set; } = new List<Exam>();
    }
}
