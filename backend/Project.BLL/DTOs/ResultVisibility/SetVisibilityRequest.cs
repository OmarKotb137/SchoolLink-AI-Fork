using Project.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs.ResultVisibility;

public class SetVisibilityRequest
{
    [Required(ErrorMessage = "Academic year ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid academic year ID")]
    public int AcademicYearId { get; set; }

    [Required(ErrorMessage = "Term is required")]
    public AcademicTerm Term { get; set; }

    public bool IsVisible { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime? VisibleFrom { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime? VisibleUntil { get; set; }

    public int ControlledById { get; set; }
}
