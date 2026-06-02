namespace Project.Domain.Entities
{
    public class GradeLevel : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public int LevelOrder { get; set; }
        public string? Stage { get; set; }
    }
}
