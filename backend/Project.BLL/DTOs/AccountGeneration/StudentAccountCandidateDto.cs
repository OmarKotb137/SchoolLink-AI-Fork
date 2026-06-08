namespace Project.BLL.DTOs.AccountGeneration;

public class StudentAccountCandidateDto
{
    public int StudentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? NationalId { get; set; }
    public string? Gender { get; set; }
    public DateTime CreatedAt { get; set; }
}
