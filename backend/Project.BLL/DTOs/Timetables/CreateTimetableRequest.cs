using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs;

public class CreateTimetableRequest
{
    [Range(1, int.MaxValue)]
    public int ClassId { get; set; }

    [Range(1, int.MaxValue)]
    public int AcademicYearId { get; set; }
}
