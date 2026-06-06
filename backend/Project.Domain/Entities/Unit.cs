namespace Project.Domain.Entities
{
    public class Unit : BaseEntity
    {
        public int SubjectId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Content { get; set; }
        public int DisplayOrder { get; set; }
        public int? PageStart { get; set; }
        public int? PageEnd { get; set; }

        public Subject Subject { get; set; } = null!;
        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    }
}
