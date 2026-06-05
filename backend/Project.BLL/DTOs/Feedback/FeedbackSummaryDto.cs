namespace Project.BLL.DTOs.Feedback;

public class FeedbackSummaryDto
{
    public double AverageRating { get; set; }
    public int TotalResponses { get; set; }
    public UnderstandingBreakdownDto UnderstandingBreakdown { get; set; } = new();
    public List<RatingTrendDto> RatingTrend { get; set; } = new();
}

public class UnderstandingBreakdownDto
{
    public int YesCount { get; set; }
    public int PartialCount { get; set; }
    public int NoCount { get; set; }
}

public class RatingTrendDto
{
    public DateOnly Date { get; set; }
    public double AverageRating { get; set; }
    public int ResponseCount { get; set; }
}
