namespace SchoolLink.Domain.Entities
{
    public class Subject : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
    }
}
