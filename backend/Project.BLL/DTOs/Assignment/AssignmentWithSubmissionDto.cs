namespace Project.BLL.DTOs.Assignment;

public class AssignmentWithSubmissionDto
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset? DueDate { get; set; }
    public decimal MaxScore { get; set; }
    public decimal? Score { get; set; }
    public string Status { get; set; } = "not-submitted";
}
