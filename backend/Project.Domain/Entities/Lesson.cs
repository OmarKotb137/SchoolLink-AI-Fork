namespace Project.Domain.Entities
{
    public class Lesson : BaseEntity
    {
        public int UnitId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }

        public Unit Unit { get; set; } = null!;
    }
}
