using System.ComponentModel.DataAnnotations;
using SchoolLink.Domain.Enums;

namespace Project.BLL.DTOs;

public class AddTimetableSlotRequest
{
    [Range(1, int.MaxValue)]
    public int TimetableId { get; set; }

    [Required]
    public SchoolDay DayOfWeek { get; set; }

    [Range(1, int.MaxValue)]
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
