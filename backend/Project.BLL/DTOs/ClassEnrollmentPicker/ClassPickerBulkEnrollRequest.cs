namespace Project.BLL.DTOs.ClassEnrollmentPicker;

/// <summary>
/// طلب تسجيل جماعي من داخل picker الفصل.
/// AcademicYearId يُستنتج من الفصل في الـ Service — لا يُرسَل من العميل.
/// </summary>
public class ClassPickerBulkEnrollRequest
{
    public int       ClassId    { get; set; }
    public List<int> StudentIds { get; set; } = new();
    public DateOnly  EnrolledAt { get; set; }
}
