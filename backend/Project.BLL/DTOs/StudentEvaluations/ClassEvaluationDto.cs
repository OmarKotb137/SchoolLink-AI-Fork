namespace Project.BLL.DTOs.StudentEvaluations;

public class ClassEvaluationDto
{
    public int EnrollmentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public IEnumerable<StudentEvaluationDto> Evaluations { get; set; } = new List<StudentEvaluationDto>();
}
