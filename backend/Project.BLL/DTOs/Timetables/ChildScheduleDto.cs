namespace Project.BLL.DTOs;

/// <summary>
/// Extends TimetableDto with student identity so the parent UI
/// can label each child's tab/card correctly.
/// </summary>
public class ChildScheduleDto : TimetableDto
{
    public int    StudentId   { get; set; }
    public string StudentName { get; set; } = string.Empty;
}
