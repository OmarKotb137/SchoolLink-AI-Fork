namespace Project.BLL.DTOs.DailyAbsences;

public class GetAbsenceFilter
{
    public int EnrollmentId { get; set; }
    public int? ClassSubjectTeacherId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}
