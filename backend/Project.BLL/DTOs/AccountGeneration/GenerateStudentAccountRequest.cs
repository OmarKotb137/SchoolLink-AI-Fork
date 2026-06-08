using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs.AccountGeneration;

public class GenerateStudentAccountRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int StudentId { get; set; }
}
