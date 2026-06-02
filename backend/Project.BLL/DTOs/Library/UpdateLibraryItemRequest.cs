namespace Project.BLL.DTOs.Library;

public class UpdateLibraryItemRequest
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int CallerUserId { get; set; }
}
