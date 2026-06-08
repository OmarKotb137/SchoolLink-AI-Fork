namespace Project.BLL.DTOs.AccountGeneration;

public class GenerateBulkStudentAccountsResultDto
{
    public int TotalRequested { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<GenerateStudentAccountResultDto> Results { get; set; } = new();
}
