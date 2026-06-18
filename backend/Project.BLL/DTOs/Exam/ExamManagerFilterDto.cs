namespace Project.BLL.DTOs.Exam;

public class ExamManagerFilterDto
{
    public string? Search { get; set; }
    public int? SubjectId { get; set; }
    public string? Status { get; set; }
    public string? SortBy { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public List<int>? CstIds { get; set; }
}
