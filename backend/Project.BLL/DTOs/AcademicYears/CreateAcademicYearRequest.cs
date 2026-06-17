using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs;

public class CreateAcademicYearRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }
    // EndDate > StartDate is validated in the service.

    public DateOnly? FirstSemesterStartDate { get; set; }
    public DateOnly? FirstSemesterEndDate { get; set; }
    public DateOnly? SecondSemesterStartDate { get; set; }
    public DateOnly? SecondSemesterEndDate { get; set; }
}
