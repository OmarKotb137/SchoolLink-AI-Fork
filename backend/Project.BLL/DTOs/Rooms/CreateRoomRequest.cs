using System.ComponentModel.DataAnnotations;
using SchoolLink.Domain.Enums;

namespace Project.BLL.DTOs;

public class CreateRoomRequest
{
    [Required]
    [MaxLength(100)]
    public string   Name     { get; set; } = string.Empty;

    [Required]
    public RoomType Type     { get; set; }

    [Range(1, 1000)]
    public int?     Capacity { get; set; }
}
