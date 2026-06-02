using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs;

public class UpdateAcademicYearRequest
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }
}
