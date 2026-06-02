namespace Project.BLL.DTOs;

public class TimetableDto
{
    public int    Id             { get; set; }
    public int    ClassId        { get; set; }
    public string ClassName      { get; set; } = string.Empty;
    public int    AcademicYearId { get; set; }
    public bool   IsActive       { get; set; }
    public List<TimetableSlotDto> Slots { get; set; } = new();
}
