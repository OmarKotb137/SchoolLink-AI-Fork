using SchoolLink.Domain.Enums;

namespace SchoolLink.Domain.Entities
{
    public class Student : BaseEntity
    {
        public string FullName { get; set; } = string.Empty;
        public string? NationalId { get; set; }
        public Gender? Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        public int? UserId { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public User? User { get; set; }
        public ICollection<StudentEnrollment> Enrollments { get; set; } = new List<StudentEnrollment>();
        public ICollection<ParentStudent> Parents { get; set; } = new List<ParentStudent>();
    }
}