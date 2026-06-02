namespace Project.BLL.DTOs;

public class GradeLevelDto
{
    public int     Id         { get; set; }
    public string  Name       { get; set; } = string.Empty;
    public string? Stage      { get; set; }
    public int     LevelOrder { get; set; }
}
