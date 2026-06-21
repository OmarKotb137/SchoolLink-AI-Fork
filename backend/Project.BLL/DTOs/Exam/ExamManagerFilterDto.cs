namespace Project.BLL.DTOs.Exam;

public class ExamManagerFilterDto
{
    public string? Search { get; set; }
    public int? SubjectId { get; set; }
    public string? Status { get; set; }
    public string? SortBy { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// معرّفات CST الخاصة بالمعلم (للأمتحانات المرتبطة بفصل محدد).
    /// </summary>
    public List<int>? CstIds { get; set; }

    /// <summary>
    /// معرّفات المواد التي يُدرّسها المعلم (للأمتحانات المنشورة للصف كله — CST=null).
    /// </summary>
    public List<int>? SubjectIds { get; set; }
}
