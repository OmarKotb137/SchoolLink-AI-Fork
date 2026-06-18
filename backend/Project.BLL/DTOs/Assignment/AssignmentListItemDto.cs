namespace Project.BLL.DTOs.Assignment;

public class AssignmentListItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public decimal MaxScore { get; set; }
    public bool IsPublished { get; set; }
    public bool IsAIGenerated { get; set; }
    public int QuestionsCount { get; set; }
    public int Submitted { get; set; }
    public int TotalStudents { get; set; }
    public decimal? AvgScore { get; set; }
    public string Status { get; set; } = "draft";
}
