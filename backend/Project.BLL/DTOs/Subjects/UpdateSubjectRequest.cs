using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs;

public class UpdateSubjectRequest
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Code { get; set; }
}
