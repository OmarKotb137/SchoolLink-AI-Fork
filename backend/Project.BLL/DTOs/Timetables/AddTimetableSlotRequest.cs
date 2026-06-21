using System.ComponentModel.DataAnnotations;
using Project.Domain.Enums;

namespace Project.BLL.DTOs;

public class AddTimetableSlotRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "معرّف الجدول غير صالح")]
    public int TimetableId { get; set; }

    [Required, EnumDataType(typeof(SchoolDay), ErrorMessage = "اليوم الدراسي غير صالح")]
    public SchoolDay DayOfWeek { get; set; }

    [Range(1, 20, ErrorMessage = "رقم الحصة يجب أن يكون بين 1 و 20")]
    public int PeriodNumber { get; set; }

    [Required]
    public TimeOnly StartTime { get; set; }

    [Required]
    public TimeOnly EndTime { get; set; }
    // EndTime > StartTime is validated in the service.
    // ClassSubjectTeacherId is required in the service when IsBreak is false.

    public int?  ClassSubjectTeacherId { get; set; }
    public bool  IsBreak               { get; set; } = false;
    public int?  RoomId               { get; set; }
}
