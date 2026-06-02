namespace Project.BLL.DTOs;

public class RoomDto
{
    public int     Id       { get; set; }
    public string  Name     { get; set; } = string.Empty;
    public string  Type     { get; set; } = string.Empty;
    public int?    Capacity { get; set; }
}
