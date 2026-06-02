namespace Project.BLL.DTOs;

public class TeacherScheduleSlotDto
{
    public int      Id                    { get; set; }
    public int      TimetableId           { get; set; }
    public int      ClassId               { get; set; }
    public string   ClassName             { get; set; } = string.Empty;
    public string   DayOfWeek             { get; set; } = string.Empty;
    public int      PeriodNumber          { get; set; }
    public TimeOnly StartTime             { get; set; }
    public TimeOnly EndTime               { get; set; }
    public int?     ClassSubjectTeacherId { get; set; }
    public string?  SubjectName           { get; set; }
    public string?  RoomName              { get; set; }
}
