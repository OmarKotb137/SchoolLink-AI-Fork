namespace Project.BLL.DTOs.Teachers;

public class TeacherDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<int> SubjectIds { get; set; } = new();
    public List<string> SubjectNames { get; set; } = new();
}
