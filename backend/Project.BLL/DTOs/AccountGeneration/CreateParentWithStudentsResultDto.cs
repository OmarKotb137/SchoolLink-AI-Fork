using Project.BLL.DTOs.Users;

namespace Project.BLL.DTOs.AccountGeneration;

public class CreateParentWithStudentsResultDto
{
    public UserDto Parent { get; set; } = null!;
    public int LinkedCount { get; set; }
    public int FailedCount { get; set; }
    public List<ChildLinkResultDto> LinkResults { get; set; } = new();
}

public class ChildLinkResultDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
