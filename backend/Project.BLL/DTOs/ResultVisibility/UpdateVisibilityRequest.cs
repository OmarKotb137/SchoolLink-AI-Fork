namespace Project.BLL.DTOs.ResultVisibility;

public class UpdateVisibilityRequest
{
    public bool IsVisible { get; set; }
    public DateTime? VisibleFrom { get; set; }
    public DateTime? VisibleUntil { get; set; }
}
