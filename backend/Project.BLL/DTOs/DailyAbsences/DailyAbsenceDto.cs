namespace Project.BLL.DTOs.DailyAbsences;

public class DailyAbsenceDto
{
    public int Id { get; set; }
    public int EnrollmentId { get; set; }
    public int? ClassSubjectTeacherId { get; set; }
    public DateOnly AbsenceDate { get; set; }
    public int? PeriodId { get; set; }
    public bool IsAbsent { get; set; }
    public string? Reason { get; set; }
    public int? RecordedById { get; set; }
    public string? SubjectName { get; set; }
}
