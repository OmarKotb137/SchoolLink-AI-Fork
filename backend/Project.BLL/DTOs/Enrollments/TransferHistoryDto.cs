namespace Project.BLL.DTOs.Enrollments;

public class TransferHistoryDto
{
    public int Id { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string FromClass { get; set; } = string.Empty;
    public string ToClass { get; set; } = string.Empty;
    public DateOnly? TransferDate { get; set; }
    public string? Reason { get; set; }
}
