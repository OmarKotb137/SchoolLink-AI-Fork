using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs;

/// <summary>
/// تحديث توقيت كل الحصص (slots) برقم حصة معيّن داخل جدول واحد (batch).
/// يُستخدم من رأس الجدول الموحّد في واجهة الأدمن لتطبيق التوقيت الجديد على عمود كامل.
/// </summary>
public class UpdatePeriodTimingRequest
{
    [Range(1, 20, ErrorMessage = "رقم الحصة يجب أن يكون بين 1 و 20")]
    public int PeriodNumber { get; set; }

    [Required(ErrorMessage = "وقت البدء مطلوب")]
    public TimeOnly StartTime { get; set; }

    [Required(ErrorMessage = "وقت الانتهاء مطلوب")]
    public TimeOnly EndTime { get; set; }
    // EndTime > StartTime is validated in the service.
}
