namespace Project.BLL.DTOs.Enrollments;

public class StudentProgressionCandidateDto
{
    public int EnrollmentId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;

    public int CurrentClassId { get; set; }
    public string CurrentClassName { get; set; } = string.Empty;

    public int CurrentGradeLevelId { get; set; }
    public string CurrentGradeLevelName { get; set; } = string.Empty;

    public int AcademicYearId { get; set; }
    public string AcademicYearName { get; set; } = string.Empty;

    public bool StudentIsActive { get; set; }
    public bool HasStudentAccount { get; set; }

    public bool HasFinalGrade { get; set; }
    public decimal? FinalTotal { get; set; }
    public bool HasPublishedFinalGrade { get; set; }
}
