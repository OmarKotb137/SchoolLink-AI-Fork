namespace Project.BLL.DTOs.AccountGeneration;

public class GenerateStudentAccountResultDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string GeneratedUsername { get; set; } = string.Empty;
    public string PlainPassword { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
