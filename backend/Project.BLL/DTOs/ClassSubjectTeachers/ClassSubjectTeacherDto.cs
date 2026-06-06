namespace Project.BLL.DTOs;

public class ClassSubjectTeacherDto
{
    public int    Id               { get; set; }
    public int    ClassId          { get; set; }
    public string ClassName        { get; set; } = string.Empty;
    public int    SubjectId        { get; set; }
    public string SubjectName      { get; set; } = string.Empty;
    public int    TeacherId        { get; set; }
    public string TeacherName      { get; set; } = string.Empty;
    public int    AcademicYearId   { get; set; }
    public string AcademicYearName { get; set; } = string.Empty;
    public int    WeeklyPeriods    { get; set; }
}
