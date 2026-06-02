using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs;

public class UpdateClassRequest
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
