using SchoolLink.Domain.Enums;

namespace Project.BLL.DTOs.ResultVisibility;

public class SetVisibilityRequest
{
    public int AcademicYearId { get; set; }
    public AcademicTerm Term { get; set; }
    public bool IsVisible { get; set; }
    public DateTime? VisibleFrom { get; set; }
    public DateTime? VisibleUntil { get; set; }
    public int ControlledById { get; set; }
}
