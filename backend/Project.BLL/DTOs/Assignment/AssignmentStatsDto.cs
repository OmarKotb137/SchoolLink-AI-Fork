namespace Project.BLL.DTOs.Assignment;

public class AssignmentStatsDto
{
    public int Total { get; set; }
    public int Open { get; set; }
    public int Closed { get; set; }
    public int Draft { get; set; }
    public int Overdue { get; set; }
    public double AvgSubmissionRate { get; set; }
}
