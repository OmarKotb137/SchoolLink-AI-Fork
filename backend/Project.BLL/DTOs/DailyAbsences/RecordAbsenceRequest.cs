namespace Project.BLL.DTOs.DailyAbsences;

public class RecordAbsenceRequest
{
    public int EnrollmentId { get; set; }
    public DateOnly AbsenceDate { get; set; }
    public bool IsAbsent { get; set; } = true;
    public int? ClassSubjectTeacherId { get; set; }
    public int? PeriodId { get; set; }
    public string? Reason { get; set; }
    public int RecordedById { get; set; }
}
