using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs;

public class UpdateTeacherAssignmentRequest
{
    [Range(1, int.MaxValue)]
    public int AssignmentId { get; set; }

    [Range(1, int.MaxValue)]
    public int TeacherId { get; set; }

    [Range(1, int.MaxValue)]
    public int WeeklyPeriods { get; set; }
}
