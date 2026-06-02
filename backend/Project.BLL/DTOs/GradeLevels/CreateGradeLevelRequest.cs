using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs;

public class CreateGradeLevelRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Stage { get; set; }
    // Stage allowed values are validated in the service.

    [Range(1, int.MaxValue)]
    public int LevelOrder { get; set; }
}
