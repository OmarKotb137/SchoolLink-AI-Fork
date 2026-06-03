namespace Project.BLL.DTOs.DailyAbsences;

public class UpdateAbsenceRequest
{
    public int AbsenceId { get; set; }
    public bool IsAbsent { get; set; }
    public string? Reason { get; set; }
}
