using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs;

public class CreateClassRequest
{
    [Range(1, int.MaxValue)]
    public int GradeLevelId { get; set; }

    [Range(1, int.MaxValue)]
    public int AcademicYearId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
