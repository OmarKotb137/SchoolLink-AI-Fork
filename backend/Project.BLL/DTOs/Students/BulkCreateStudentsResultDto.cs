namespace Project.BLL.DTOs.Students;

public class BulkCreateStudentsResultDto
{
    public int TotalRequested { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<StudentDto> CreatedStudents { get; set; } = new();
    public List<BulkCreateStudentFailureDto> Failures { get; set; } = new();
}

public class BulkCreateStudentFailureDto
{
    public int Index { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
