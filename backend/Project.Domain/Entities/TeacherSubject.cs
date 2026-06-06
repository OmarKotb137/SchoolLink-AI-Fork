namespace Project.Domain.Entities
{
    public class TeacherSubject : BaseEntity
    {
        public int TeacherId { get; set; }
        public int SubjectId { get; set; }

        public User Teacher { get; set; } = null!;
        public Subject Subject { get; set; } = null!;
    }
}
