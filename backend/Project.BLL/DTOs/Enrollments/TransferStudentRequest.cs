namespace Project.BLL.DTOs.Enrollments;

public class TransferStudentRequest
{
    public int CurrentEnrollmentId { get; set; }
    public int NewClassId { get; set; }
    public DateOnly TransferDate { get; set; }
    public string? TransferReason { get; set; }
}
