using Project.Domain.Enums;

namespace Project.BLL.DTOs.EvaluationPeriods;

public class UpdateEvaluationPeriodRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public PeriodType PeriodType { get; set; }
    public int OrderNum { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? MonthName { get; set; }
}
