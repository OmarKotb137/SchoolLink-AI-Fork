namespace Project.BLL.DTOs;

public class TimetableSlotDto
{
    public int      Id                     { get; set; }
    public int      TimetableId            { get; set; }
    public string   DayOfWeek              { get; set; } = string.Empty;
    public int      PeriodNumber           { get; set; }
    public TimeOnly StartTime              { get; set; }
    public TimeOnly EndTime                { get; set; }
    public bool     IsBreak                { get; set; }
    public int?     ClassSubjectTeacherId  { get; set; }
    public string?  SubjectName            { get; set; }
    public string?  TeacherName            { get; set; }
}
