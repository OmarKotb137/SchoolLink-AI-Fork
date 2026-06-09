namespace Project.BLL.DTOs.ClassEnrollmentPicker;

/// <summary>
/// فلتر البحث عن الطلاب الغير مسجلين.
/// </summary>
public class GetAvailableStudentsFilter
{
    public int     Page           { get; set; } = 1;
    public int     PageSize       { get; set; } = 20;
    public string? SearchTerm     { get; set; }
    public DateOnly? BirthDateFrom { get; set; }
    public DateOnly? BirthDateTo   { get; set; }
    public string? SortBy         { get; set; } = "name";
    public bool    SortDescending { get; set; } = false;
}
