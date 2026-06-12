namespace Project.BLL.DTOs.FinalGrades;

public class CalculateFullFinalGradesRequest
{
    public int ClassId { get; set; }
    public List<StudentFinalGradeInput> Students { get; set; } = new();
}

public class StudentFinalGradeInput
{
    public int EnrollmentId { get; set; }
    public decimal? MonthlyExam1Score { get; set; }
    public decimal? MonthlyExam2Score { get; set; }
    public decimal? SemesterExamScore { get; set; }
}
