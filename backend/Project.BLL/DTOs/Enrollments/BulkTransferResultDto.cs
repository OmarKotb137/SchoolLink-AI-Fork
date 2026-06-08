namespace Project.BLL.DTOs.Enrollments;

public class BulkTransferResultDto
{
    public int TotalRequested { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<BulkTransferFailureDto> Failures { get; set; } = new();
}

public class BulkTransferFailureDto
{
    public int EnrollmentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}