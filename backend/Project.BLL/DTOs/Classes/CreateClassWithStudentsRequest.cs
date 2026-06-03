using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs;

public class CreateClassWithStudentsRequest
{
    [Required][MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(100)]
    public string Teacher { get; set; } = string.Empty;
    [MaxLength(100)]
    public string Subject { get; set; } = string.Empty;
    [MaxLength(20)]
    public string Year { get; set; } = string.Empty;
    public int TemplateId { get; set; }
    public List<string> Students { get; set; } = new();
}
