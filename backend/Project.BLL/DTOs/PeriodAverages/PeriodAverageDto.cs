namespace Project.BLL.DTOs.PeriodAverages;

public class PeriodAverageDto
{
    public int Id { get; set; }
    public int EnrollmentId { get; set; }
    public int PeriodId { get; set; }
    public decimal AvgScore { get; set; }
    public decimal MaxScore { get; set; }
    public DateTime CalculatedAt { get; set; }
    public string? PeriodName { get; set; }
    public string? PeriodType { get; set; }
}
