using Project.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs.AccountGeneration;

public class CreateParentWithStudentsRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [RegularExpression(@"^(01[0-2,5]\d{8}|0[2-9]\d{7,8})$")]
    public string? Phone { get; set; }

    public List<ParentChildLinkRequest> Children { get; set; } = new();
}

public class ParentChildLinkRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int StudentId { get; set; }

    [Required]
    [Range(1, 5)]
    public RelationshipType Relationship { get; set; }
}
