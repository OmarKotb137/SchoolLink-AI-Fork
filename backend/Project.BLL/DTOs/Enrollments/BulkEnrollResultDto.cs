namespace Project.BLL.DTOs.Enrollments;

public class BulkEnrollResultDto
{
    public int TotalRequested { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<BulkEnrollFailureDto> Failures { get; set; } = new();
}

public class BulkEnrollFailureDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
