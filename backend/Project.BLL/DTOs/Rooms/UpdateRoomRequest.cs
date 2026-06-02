using System.ComponentModel.DataAnnotations;
using Project.Domain.Enums;

namespace Project.BLL.DTOs;

public class UpdateRoomRequest
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string   Name     { get; set; } = string.Empty;

    [Required]
    public RoomType Type     { get; set; }

    [Range(1, 1000)]
    public int?     Capacity { get; set; }
}
