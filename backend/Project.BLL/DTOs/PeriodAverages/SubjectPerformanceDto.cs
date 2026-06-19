namespace Project.BLL.DTOs.PeriodAverages;

public class SubjectPerformanceDto
{
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public int PeriodId { get; set; }
    public string? PeriodName { get; set; }
    public string? PeriodType { get; set; }
    public decimal AvgScore { get; set; }
    public decimal MaxScore { get; set; }
}

public class SubjectPerformanceGroupDto
{
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public List<SubjectPerformanceDto> Periods { get; set; } = new();
}
