using System.ComponentModel.DataAnnotations;
using Project.Domain.Enums;

namespace Project.BLL.DTOs;

public class UpdateTimetableSlotRequest
{
    [Range(1, int.MaxValue)]
    public int SlotId { get; set; }

    [Required]
    public SchoolDay DayOfWeek { get; set; }

    [Range(1, int.MaxValue)]
    public int PeriodNumber { get; set; }

    [Required]
    public TimeOnly StartTime { get; set; }

    [Required]
    public TimeOnly EndTime { get; set; }
    // EndTime > StartTime is validated in the service.

    public int?  ClassSubjectTeacherId { get; set; }
    public bool  IsBreak               { get; set; }
}
