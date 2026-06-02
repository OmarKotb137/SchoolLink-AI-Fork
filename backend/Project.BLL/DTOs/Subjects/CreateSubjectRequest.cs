using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs;

public class CreateSubjectRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Code { get; set; }
}
