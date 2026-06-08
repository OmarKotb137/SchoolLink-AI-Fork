namespace Project.BLL.DTOs.Enrollments;

public class BulkTransferStudentsRequest
{
    public List<int> EnrollmentIds { get; set; } = new();
    public int NewClassId { get; set; }
    public DateOnly TransferDate { get; set; }
    public string? TransferReason { get; set; }
}