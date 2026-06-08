using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs.AccountGeneration;

public class BulkStudentAccountsRequest
{
    [Required]
    public List<int> StudentIds { get; set; } = new();
}
