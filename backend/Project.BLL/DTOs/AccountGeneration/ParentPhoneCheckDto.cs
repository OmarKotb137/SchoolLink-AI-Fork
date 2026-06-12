namespace Project.BLL.DTOs.AccountGeneration;

public class ParentPhoneCheckDto
{
    public bool AlreadyExists { get; set; }
    public int? ExistingParentId { get; set; }
    public string? ExistingParentName { get; set; }
    public string? ExistingParentUsername { get; set; }
}
