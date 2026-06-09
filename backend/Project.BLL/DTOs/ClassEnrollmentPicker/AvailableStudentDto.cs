namespace Project.BLL.DTOs.ClassEnrollmentPicker;

/// <summary>
/// بيانات طالب متاح للتسجيل في فصل (غير مسجل في أي فصل حالياً).
/// </summary>
public class AvailableStudentDto
{
    public int      Id         { get; set; }
    public string   FullName   { get; set; } = string.Empty;
    public string?  NationalId { get; set; }
    public int?     Gender     { get; set; }
    public DateOnly? BirthDate { get; set; }
}
