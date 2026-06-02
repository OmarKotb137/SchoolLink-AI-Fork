using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs;

public class AssignTeacherRequest
{
    [Range(1, int.MaxValue)]
    public int ClassId { get; set; }

    [Range(1, int.MaxValue)]
    public int SubjectId { get; set; }

    [Range(1, int.MaxValue)]
    public int TeacherId { get; set; }

    [Range(1, int.MaxValue)]
    public int AcademicYearId { get; set; }
}
