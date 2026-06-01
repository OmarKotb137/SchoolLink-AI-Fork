namespace SchoolLink.Domain.Entities
{
    public class SchoolProfile : BaseEntity
    {
        public string SchoolName { get; set; } = string.Empty;
        public string Governorate { get; set; } = string.Empty;
        public string Directorate { get; set; } = string.Empty;
        public string EducationalAdministration { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? ManagerName { get; set; }
        public string? LogoPath { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
