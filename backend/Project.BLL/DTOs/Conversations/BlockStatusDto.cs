namespace Project.BLL.DTOs.Conversations;

public class BlockStatusDto
{
    public bool IsBlocked { get; set; }
    public bool BlockedByMe { get; set; }
    public bool BlockedByOther { get; set; }
}
