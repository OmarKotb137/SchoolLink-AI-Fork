using Project.Domain.Enums;

namespace Project.Domain.Entities
{
    public class ParentStudent : BaseEntity
    {
        public int ParentId { get; set; }
        public int StudentId { get; set; }
        public RelationshipType Relationship { get; set; }

        // Navigation Properties
        public User Parent { get; set; } = null!;
        public Student Student { get; set; } = null!;
    }
}
